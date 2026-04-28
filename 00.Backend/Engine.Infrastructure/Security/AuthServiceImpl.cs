using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Engine.Application.Auth;
using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Domain.Organization;
using Engine.Infrastructure.Persistence.Mongo;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace Engine.Infrastructure.Security;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

public class AuthServiceImpl : IAuthService
{
    private readonly GroupwareDbContext _db;
    private readonly JwtSettings _jwt;
    private readonly IAuditLogService _audit;

    public AuthServiceImpl(GroupwareDbContext db, IOptions<JwtSettings> jwtOptions, IAuditLogService audit)
    {
        _db = db;
        _jwt = jwtOptions.Value;
        _audit = audit;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Find(u => u.Email == request.Email && !u.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        if (user.IsLocked)
            throw new UnauthorizedException("잠긴 계정입니다. 관리자에게 문의하세요.");

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            user.LoginFailCount++;
            if (user.LoginFailCount >= 5)
            {
                user.IsLocked = true;
                user.LockedAt = DateTime.UtcNow;
            }
            await _db.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
            await _audit.LogAsync("LOGIN_FAIL", "User", user.Id, false, "잘못된 비밀번호", ct: ct);
            throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");
        }

        // 로그인 성공
        user.LoginFailCount = 0;
        user.LastLoginAt = DateTime.UtcNow;

        var (accessToken, accessExpiry) = GenerateAccessToken(user);
        var (refreshToken, refreshExpiry) = GenerateRefreshToken();

        user.RefreshTokenHash = HashToken(refreshToken);
        user.RefreshTokenExpiry = refreshExpiry;

        await _db.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
        await _audit.LogAsync("LOGIN", "User", user.Id, ct: ct);

        var roles = user.Roles;

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = accessExpiry,
            RefreshTokenExpiry = refreshExpiry,
            User = new UserProfileDto
            {
                Id = user.Id,
                EmployeeNo = user.EmployeeNo,
                Name = user.Name,
                Email = user.Email,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.DepartmentName,
                PositionName = user.PositionName,
                TitleName = user.TitleName,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = roles,
                Permissions = await GetUserPermissionsAsync(user.Id, roles, ct)
            }
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var user = await _db.Users
            .Find(u => u.RefreshTokenHash == tokenHash && !u.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedException("유효하지 않은 Refresh Token입니다.");

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh Token이 만료되었습니다.");

        var (accessToken, accessExpiry) = GenerateAccessToken(user);
        var (newRefreshToken, refreshExpiry) = GenerateRefreshToken();

        user.RefreshTokenHash = HashToken(newRefreshToken);
        user.RefreshTokenExpiry = refreshExpiry;

        await _db.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);

        var roles = user.Roles;
        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiry = accessExpiry,
            RefreshTokenExpiry = refreshExpiry,
            User = new UserProfileDto
            {
                Id = user.Id,
                EmployeeNo = user.EmployeeNo,
                Name = user.Name,
                Email = user.Email,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.DepartmentName,
                PositionName = user.PositionName,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = roles,
                Permissions = await GetUserPermissionsAsync(user.Id, roles, ct)
            }
        };
    }

    public async Task LogoutAsync(string userId, CancellationToken ct = default)
    {
        await _db.Users.UpdateOneAsync(
            u => u.Id == userId,
            Builders<User>.Update
                .Set(u => u.RefreshTokenHash, null)
                .Set(u => u.RefreshTokenExpiry, null),
            cancellationToken: ct);

        await _audit.LogAsync("LOGOUT", "User", userId, ct: ct);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new AppException("VALIDATION_ERROR", "새 비밀번호가 일치하지 않습니다.");

        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User", userId);

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new AppException("VALIDATION_ERROR", "현재 비밀번호가 올바르지 않습니다.", 400);

        user.PasswordHash = HashPassword(request.NewPassword);
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiry = null;

        await _db.Users.ReplaceOneAsync(u => u.Id == userId, user, cancellationToken: ct);
    }

    // ─── 내부 헬퍼 ───

    private (string token, DateTime expiry) GenerateAccessToken(User user)
    {
        var expiry = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.Name),
            new("departmentId", user.DepartmentId),
            new("departmentName", user.DepartmentName),
            new("positionName", user.PositionName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private static (string token, DateTime expiry) GenerateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (token, DateTime.UtcNow.AddDays(7));
    }

    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 11);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private async Task<List<string>> GetUserPermissionsAsync(string userId, List<string> roles, CancellationToken ct)
    {
        var permissions = new HashSet<string>();
        foreach (var role in roles)
        {
            var rp = await _db.RolePermissions
                .Find(r => r.RoleName == role)
                .FirstOrDefaultAsync(ct);
            rp?.Permissions.ForEach(p => permissions.Add(p));
        }
        return [.. permissions];
    }
}

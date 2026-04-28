using Engine.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

public class AuthController : BaseController
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>로그인</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, null, ct));

    /// <summary>토큰 갱신</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
        => Ok(await _auth.RefreshTokenAsync(request, ct));

    /// <summary>로그아웃</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Extract userId from token claims
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        await _auth.LogoutAsync(userId, ct);
        return NoContent("로그아웃 되었습니다.");
    }

    /// <summary>내 프로필 조회</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        // Return basic claim info since IAuthService doesn't have GetProfileAsync
        return Ok(new { UserId = userId, Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value });
    }

    /// <summary>비밀번호 변경</summary>
    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        await _auth.ChangePasswordAsync(userId, request, ct);
        return NoContent("비밀번호가 변경되었습니다.");
    }
}

using Engine.Application.Common.DTOs;
using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Application.Organization;
using Engine.Domain.Common.Enums;
using Engine.Domain.Organization;
using Engine.Infrastructure.Persistence.Mongo;
using Engine.Infrastructure.Security;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

public class OrganizationServiceImpl : IOrganizationService
{
    private readonly GroupwareDbContext _db;

    public OrganizationServiceImpl(GroupwareDbContext db)
    {
        _db = db;
    }

    public async Task<List<OrganizationDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var all = await _db.Organizations
            .Find(o => !o.IsDeleted && o.IsActive)
            .SortBy(o => o.SortOrder)
            .ToListAsync(ct);

        return BuildTree(all, null);
    }

    public async Task<OrganizationDto> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var org = await _db.Organizations.Find(o => o.Id == id && !o.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Organization", id);
        return ToDto(org);
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        var entity = new Organization
        {
            Code = request.Code,
            Name = request.Name,
            ParentId = request.ParentId,
            DeptType = request.DeptType,
            SortOrder = request.SortOrder,
            Level = string.IsNullOrEmpty(request.ParentId) ? 1 : await GetLevelAsync(request.ParentId, ct) + 1
        };

        await _db.Organizations.InsertOneAsync(entity, cancellationToken: ct);
        return ToDto(entity);
    }

    public async Task<OrganizationDto> UpdateAsync(string id, UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Organizations.Find(o => o.Id == id && !o.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Organization", id);

        entity.Name = request.Name;
        entity.ManagerUserId = request.ManagerUserId;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.Organizations.ReplaceOneAsync(o => o.Id == id, entity, cancellationToken: ct);
        return ToDto(entity);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var update = Builders<Organization>.Update
            .Set(o => o.IsDeleted, true)
            .Set(o => o.DeletedAt, DateTime.UtcNow);
        await _db.Organizations.UpdateOneAsync(o => o.Id == id, update, cancellationToken: ct);
    }

    private async Task<int> GetLevelAsync(string parentId, CancellationToken ct)
    {
        var parent = await _db.Organizations.Find(o => o.Id == parentId).FirstOrDefaultAsync(ct);
        return parent?.Level ?? 0;
    }

    private static List<OrganizationDto> BuildTree(List<Organization> all, string? parentId)
    {
        return all.Where(o => o.ParentId == parentId)
            .Select(o => new OrganizationDto
            {
                Id = o.Id,
                Code = o.Code,
                Name = o.Name,
                ParentId = o.ParentId,
                DeptType = o.DeptType,
                SortOrder = o.SortOrder,
                Level = o.Level,
                ManagerUserId = o.ManagerUserId,
                IsActive = o.IsActive,
                Children = BuildTree(all, o.Id)
            }).ToList();
    }

    private static OrganizationDto ToDto(Organization o) => new()
    {
        Id = o.Id,
        Code = o.Code,
        Name = o.Name,
        ParentId = o.ParentId,
        DeptType = o.DeptType,
        SortOrder = o.SortOrder,
        Level = o.Level,
        ManagerUserId = o.ManagerUserId,
        IsActive = o.IsActive,
    };
}

public class UserServiceImpl : IUserService
{
    private readonly GroupwareDbContext _db;

    public UserServiceImpl(GroupwareDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserDto>> SearchAsync(UserSearchRequest request, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.DepartmentId))
            filter &= Builders<User>.Filter.Eq(u => u.DepartmentId, request.DepartmentId);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
            filter &= Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.Name, new MongoDB.Bson.BsonRegularExpression(request.Keyword, "i")),
                Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(request.Keyword, "i")),
                Builders<User>.Filter.Regex(u => u.EmployeeNo, new MongoDB.Bson.BsonRegularExpression(request.Keyword, "i"))
            );
        if (request.Status.HasValue)
            filter &= Builders<User>.Filter.Eq(u => u.EmploymentStatus, request.Status.Value);

        var total = await _db.Users.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.Users.Find(filter)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = (int)total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<UserDto>> GetByDepartmentAsync(string departmentId, CancellationToken ct = default)
    {
        var users = await _db.Users.Find(u => u.DepartmentId == departmentId && !u.IsDeleted).ToListAsync(ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await _db.Users.Find(u => u.Id == id && !u.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User", id);
        return ToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var existing = await _db.Users.Find(u => u.Email == request.Email).FirstOrDefaultAsync(ct);
        if (existing != null) throw new ConflictException($"이미 사용 중인 이메일입니다: {request.Email}");

        var user = new User
        {
            EmployeeNo = request.EmployeeNo,
            Name = request.Name,
            Email = request.Email,
            Mobile = request.Mobile,
            DepartmentId = request.DepartmentId,
            PositionCode = request.PositionCode,
            TitleCode = request.TitleCode,
            GradeCode = request.GradeCode,
            HiredAt = request.HiredAt,
            PasswordHash = AuthServiceImpl.HashPassword(request.TempPassword),
            Roles = ["User"]
        };

        // 부서명 조회
        var dept = await _db.Organizations.Find(o => o.Id == request.DepartmentId).FirstOrDefaultAsync(ct);
        user.DepartmentName = dept?.Name ?? string.Empty;

        await _db.Users.InsertOneAsync(user, cancellationToken: ct);
        return ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.Find(u => u.Id == id && !u.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User", id);

        user.Mobile = request.Mobile;
        user.DepartmentId = request.DepartmentId;
        user.PositionCode = request.PositionCode;
        user.TitleCode = request.TitleCode;
        user.GradeCode = request.GradeCode;
        user.EmploymentStatus = request.EmploymentStatus;
        user.ResignedAt = request.ResignedAt;
        user.UpdatedAt = DateTime.UtcNow;

        var dept = await _db.Organizations.Find(o => o.Id == request.DepartmentId).FirstOrDefaultAsync(ct);
        user.DepartmentName = dept?.Name ?? string.Empty;

        await _db.Users.ReplaceOneAsync(u => u.Id == id, user, cancellationToken: ct);
        return ToDto(user);
    }

    public async Task DeactivateAsync(string id, CancellationToken ct = default)
    {
        await _db.Users.UpdateOneAsync(
            u => u.Id == id,
            Builders<User>.Update
                .Set(u => u.EmploymentStatus, EmploymentStatus.Resigned)
                .Set(u => u.IsDeleted, true)
                .Set(u => u.DeletedAt, DateTime.UtcNow),
            cancellationToken: ct);
    }

    public async Task AssignRoleAsync(string userId, string roleId, CancellationToken ct = default)
    {
        await _db.Users.UpdateOneAsync(
            u => u.Id == userId,
            Builders<User>.Update.AddToSet(u => u.Roles, roleId),
            cancellationToken: ct);
    }

    public async Task RemoveRoleAsync(string userId, string roleId, CancellationToken ct = default)
    {
        await _db.Users.UpdateOneAsync(
            u => u.Id == userId,
            Builders<User>.Update.Pull(u => u.Roles, roleId),
            cancellationToken: ct);
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        EmployeeNo = u.EmployeeNo,
        Name = u.Name,
        Email = u.Email,
        Mobile = u.Mobile,
        DepartmentId = u.DepartmentId,
        DepartmentName = u.DepartmentName,
        PositionCode = u.PositionCode,
        PositionName = u.PositionName,
        TitleName = u.TitleName,
        GradeName = u.GradeName,
        ProfileImageUrl = u.ProfileImageUrl,
        EmploymentStatus = u.EmploymentStatus,
        HiredAt = u.HiredAt,
        Roles = u.Roles
    };
}

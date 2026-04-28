using Engine.Application.Common.DTOs;
using Engine.Application.Common.Responses;
using Engine.Domain.Common.Enums;

namespace Engine.Application.Organization;

// ─── Organization DTOs ───

public class OrganizationDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string DeptType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Level { get; set; }
    public string? ManagerUserId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public List<OrganizationDto> Children { get; set; } = new();
}

public class CreateOrganizationRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string DeptType { get; set; } = "TEAM";
    public int SortOrder { get; set; } = 0;
}

public class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ManagerUserId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

// ─── User DTOs ───

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? TitleName { get; set; }
    public string? GradeName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }
    public DateTime HiredAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class CreateUserRequest
{
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TempPassword { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string? TitleCode { get; set; }
    public string? GradeCode { get; set; }
    public DateTime HiredAt { get; set; }
}

public class UpdateUserRequest
{
    public string? Mobile { get; set; }
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string? TitleCode { get; set; }
    public string? GradeCode { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }
    public DateTime? ResignedAt { get; set; }
}

public class UserSearchRequest : PagedRequest
{
    public string? DepartmentId { get; set; }
    public EmploymentStatus? Status { get; set; }
}

// ─── Service Interfaces ───

public interface IOrganizationService
{
    Task<List<OrganizationDto>> GetTreeAsync(CancellationToken ct = default);
    Task<OrganizationDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default);
    Task<OrganizationDto> UpdateAsync(string id, UpdateOrganizationRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public interface IUserService
{
    Task<PagedResult<UserDto>> SearchAsync(UserSearchRequest request, CancellationToken ct = default);
    Task<List<UserDto>> GetByDepartmentAsync(string departmentId, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(string id, UpdateUserRequest request, CancellationToken ct = default);
    Task DeactivateAsync(string id, CancellationToken ct = default);
    Task AssignRoleAsync(string userId, string roleId, CancellationToken ct = default);
    Task RemoveRoleAsync(string userId, string roleId, CancellationToken ct = default);
}

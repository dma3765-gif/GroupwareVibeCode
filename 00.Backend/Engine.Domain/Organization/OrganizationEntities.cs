using Engine.Domain.Common.Entities;
using Engine.Domain.Common.Enums;

namespace Engine.Domain.Organization;

/// <summary>
/// 조직(부서) 도메인 엔티티
/// </summary>
public class Organization : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string DeptType { get; set; } = "TEAM"; // COMPANY, DIVISION, TEAM 등
    public int SortOrder { get; set; } = 0;
    public int Level { get; set; } = 1;
    public string? ManagerUserId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<OrganizationHistory> ChangeHistories { get; set; } = new();
}

public class OrganizationHistory
{
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // RENAME, MOVE, CREATE, CLOSE
    public string? Remark { get; set; }
}

/// <summary>
/// 사용자 도메인 엔티티
/// </summary>
public class User : BaseEntity
{
    public string EmployeeNo { get; set; } = string.Empty; // 사번
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Upn { get; set; } // Azure AD UPN
    public string? Mobile { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty; // 직위 코드
    public string PositionName { get; set; } = string.Empty; // 직위명
    public string? TitleCode { get; set; }   // 직책 코드 (팀장, 부장 등)
    public string? TitleName { get; set; }
    public string? GradeCode { get; set; }   // 직급
    public string? GradeName { get; set; }
    public DateTime HiredAt { get; set; }
    public DateTime? ResignedAt { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;
    public string? ProfileImageUrl { get; set; }
    public List<ConcurrentPosition> ConcurrentPositions { get; set; } = new(); // 겸직
    public List<string> Roles { get; set; } = new();
    public bool IsLocked { get; set; } = false;
    public int LoginFailCount { get; set; } = 0;
    public DateTime? LockedAt { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>겸직 정보</summary>
public class ConcurrentPosition
{
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>권한/역할</summary>
public class RolePermission : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public SystemRole Role { get; set; }
    public List<string> Permissions { get; set; } = new();
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = false;
}

/// <summary>사용자-역할 매핑</summary>
public class UserRole : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public SystemRole Role { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

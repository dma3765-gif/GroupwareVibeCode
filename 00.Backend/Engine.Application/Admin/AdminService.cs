using Engine.Application.Common.DTOs;
using Engine.Application.Common.Responses;

namespace Engine.Application.Admin;

// ─── System Code DTOs ───

public class SystemCodeDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class UpsertSystemCodeRequest
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// ─── Menu DTOs ───

public class MenuDto
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public string? Url { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
    public List<string> RequiredRoles { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public List<MenuDto> Children { get; set; } = new();
}

public class UpsertMenuRequest
{
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public string? Url { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public List<string> RequiredRoles { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
}

// ─── System Settings DTOs ───

public class SystemSettingDto
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public bool IsSecret { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class UpdateSystemSettingRequest
{
    public string Value { get; set; } = string.Empty;
}

// ─── Audit Log DTOs ───

public class AuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string? ActorName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public object? Before { get; set; }
    public object? After { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogQuery : PagedRequest
{
    public string? Action { get; set; }
    public string? ActorUserId { get; set; }
    public string? ResourceType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

// ─── Role/Permission DTOs ───

public class RoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class AssignRoleRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// ─── Service Interfaces ───

public interface ISystemCodeService
{
    Task<PagedResult<SystemCodeDto>> GetCodesAsync(string? groupCode, PagedRequest request, CancellationToken ct = default);
    Task<List<SystemCodeDto>> GetCodesByGroupAsync(string groupCode, CancellationToken ct = default);
    Task<SystemCodeDto> GetCodeByIdAsync(string id, CancellationToken ct = default);
    Task<SystemCodeDto> UpsertCodeAsync(UpsertSystemCodeRequest request, CancellationToken ct = default);
    Task DeleteCodeAsync(string id, CancellationToken ct = default);
}

public interface IMenuService
{
    Task<List<MenuDto>> GetMenuTreeAsync(CancellationToken ct = default);
    Task<List<MenuDto>> GetMyMenusAsync(CancellationToken ct = default);
    Task<MenuDto> GetMenuByIdAsync(string id, CancellationToken ct = default);
    Task<MenuDto> CreateMenuAsync(UpsertMenuRequest request, CancellationToken ct = default);
    Task<MenuDto> UpdateMenuAsync(string id, UpsertMenuRequest request, CancellationToken ct = default);
    Task DeleteMenuAsync(string id, CancellationToken ct = default);
}

public interface ISystemSettingService
{
    Task<List<SystemSettingDto>> GetAllSettingsAsync(string? category, CancellationToken ct = default);
    Task<SystemSettingDto> GetSettingByKeyAsync(string key, CancellationToken ct = default);
    Task<SystemSettingDto> UpdateSettingAsync(string key, UpdateSystemSettingRequest request, CancellationToken ct = default);
}

public interface IAuditLogQueryService
{
    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogQuery query, CancellationToken ct = default);
}

using Engine.Domain.Common.Entities;

namespace Engine.Domain.Security;

/// <summary>감사 로그</summary>
public class AuditLog
{
    public string Id { get; set; } = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
    public string TenantId { get; set; } = "default";
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? FailureReason { get; set; }
    public Dictionary<string, object?>? Before { get; set; }
    public Dictionary<string, object?>? After { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>파일 메타데이터</summary>
public class FileMetadata : BaseEntity
{
    public string OriginalName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string StorageType { get; set; } = "Local"; // Local, S3, Azure
    public string UploaderUserId { get; set; } = string.Empty;
    public string? LinkedEntityType { get; set; }
    public string? LinkedEntityId { get; set; }
    public bool IsPublic { get; set; } = false;
    public int DownloadCount { get; set; } = 0;
}

/// <summary>시스템 코드</summary>
public class SystemCode : BaseEntity
{
    /// <summary>코드 그룹 (예: LEAVE_TYPE, POSITION_LEVEL)</summary>
    public string GroupCode { get; set; } = string.Empty;
    /// <summary>코드 그룹명</summary>
    public string GroupName { get; set; } = string.Empty;
    /// <summary>코드 값</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>코드 명칭</summary>
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Dictionary<string, string>? Extra { get; set; }
}

/// <summary>메뉴</summary>
public class Menu : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? Route { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public List<string> RequiredPermissions { get; set; } = new();
    public List<string> RequiredRoles { get; set; } = new();
}

/// <summary>시스템 설정</summary>
public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public bool IsSecret { get; set; } = false;
    public string Category { get; set; } = "General";
}

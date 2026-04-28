using Engine.Domain.Common.Entities;
using Engine.Domain.Common.Enums;

namespace Engine.Domain.Notification;

/// <summary>알림</summary>
public class Notification : BaseEntity
{
    public string RecipientUserId { get; set; } = string.Empty;
    /// <summary>수신자 ID 별칭 (RecipientUserId)</summary>
    public string ReceiverId { get => RecipientUserId; set => RecipientUserId = value; }
    public NotificationType Type { get; set; }
    /// <summary>알림 유형 문자열 (Type 별칭)</summary>
    public string NotificationType { get => Type.ToString(); set { if (Enum.TryParse<NotificationType>(value, out var t)) Type = t; } }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>본문 별칭 (Message)</summary>
    public string? Body { get => Message; set => Message = value ?? string.Empty; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    /// <summary>대상 리소스 ID 별칭 (ResourceId)</summary>
    public string? TargetId { get => ResourceId; set => ResourceId = value; }
    public string? Url { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Web;
}

/// <summary>알림 템플릿</summary>
public class NotificationTemplate : BaseEntity
{
    public NotificationType Type { get; set; }
    public string TitleTemplate { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>사용자별 알림 수신 설정</summary>
public class NotificationSetting : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = false;
    public bool PushEnabled { get; set; } = false;
}

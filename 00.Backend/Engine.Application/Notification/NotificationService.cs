using Engine.Application.Common.DTOs;
using Engine.Application.Common.Responses;

namespace Engine.Application.Notification;

// ─── DTOs ───

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationQuery : PagedRequest
{
    public string? Type { get; set; }
    public bool? IsRead { get; set; }
}

// ─── Service Interface ───

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(NotificationQuery query, CancellationToken ct = default);
    Task<long> GetUnreadCountAsync(CancellationToken ct = default);
    Task MarkAsReadAsync(string notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(CancellationToken ct = default);
    Task DeleteNotificationAsync(string notificationId, CancellationToken ct = default);
}


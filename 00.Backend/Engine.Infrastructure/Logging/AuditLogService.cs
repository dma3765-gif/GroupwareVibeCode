using Engine.Application.Common.Interfaces;
using Engine.Domain.Common.Enums;
using Engine.Domain.Notification;
using Engine.Domain.Security;
using Engine.Infrastructure.Persistence.Mongo;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Engine.Infrastructure.Logging;

public class AuditLogService : IAuditLogService
{
    private readonly GroupwareDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserContext _currentUser;

    public AuditLogService(GroupwareDbContext db, IHttpContextAccessor httpContextAccessor, ICurrentUserContext currentUser)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string resourceType, string? resourceId,
        bool isSuccess = true, string? failureReason = null,
        object? before = null, object? after = null,
        CancellationToken ct = default)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var ua = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

        var log = new AuditLog
        {
            TenantId = _currentUser.IsAuthenticated ? _currentUser.TenantId : "default",
            ActorUserId = _currentUser.IsAuthenticated ? _currentUser.UserId : "system",
            ActorName = _currentUser.IsAuthenticated ? _currentUser.Name : "System",
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            IpAddress = ip,
            UserAgent = ua,
            IsSuccess = isSuccess,
            FailureReason = failureReason,
            Before = before != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(
                System.Text.Json.JsonSerializer.Serialize(before)) : null,
            After = after != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(
                System.Text.Json.JsonSerializer.Serialize(after)) : null,
        };

        await _db.AuditLogs.InsertOneAsync(log, cancellationToken: ct);
    }
}

public class NotificationPublisher : INotificationPublisher
{
    private readonly GroupwareDbContext _db;

    public NotificationPublisher(GroupwareDbContext db)
    {
        _db = db;
    }

    public async Task PublishAsync(string recipientUserId, NotificationType type,
        string title, string message, string? resourceType = null, string? resourceId = null,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Message = message,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Channel = NotificationChannel.Web
        };

        await _db.Notifications.InsertOneAsync(notification, cancellationToken: ct);
    }
}

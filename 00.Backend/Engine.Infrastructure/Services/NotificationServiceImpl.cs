using Engine.Application.Common.DTOs;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Application.Notification;
using Engine.Domain.Notification;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

public class NotificationServiceImpl : INotificationService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public NotificationServiceImpl(GroupwareDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(NotificationQuery query, CancellationToken ct = default)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.ReceiverId, _currentUser.UserId),
            Builders<Notification>.Filter.Eq(n => n.IsDeleted, false)
        );

        if (query.IsRead.HasValue)
            filter &= Builders<Notification>.Filter.Eq(n => n.IsRead, query.IsRead.Value);
        if (!string.IsNullOrEmpty(query.Type))
            filter &= Builders<Notification>.Filter.Eq(n => n.NotificationType, query.Type);

        var total = await _db.Notifications.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.Notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<NotificationDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = (int)total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<long> GetUnreadCountAsync(CancellationToken ct = default)
    {
        return await _db.Notifications.CountDocumentsAsync(
            n => n.ReceiverId == _currentUser.UserId && !n.IsRead && !n.IsDeleted,
            cancellationToken: ct);
    }

    public async Task MarkAsReadAsync(string notificationId, CancellationToken ct = default)
    {
        await _db.Notifications.UpdateOneAsync(
            n => n.Id == notificationId && n.ReceiverId == _currentUser.UserId,
            Builders<Notification>.Update
                .Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow),
            cancellationToken: ct);
    }

    public async Task MarkAllAsReadAsync(CancellationToken ct = default)
    {
        await _db.Notifications.UpdateManyAsync(
            n => n.ReceiverId == _currentUser.UserId && !n.IsRead,
            Builders<Notification>.Update
                .Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow),
            cancellationToken: ct);
    }

    public async Task DeleteNotificationAsync(string notificationId, CancellationToken ct = default)
    {
        await _db.Notifications.UpdateOneAsync(
            n => n.Id == notificationId && n.ReceiverId == _currentUser.UserId,
            Builders<Notification>.Update.Set(n => n.IsDeleted, true),
            cancellationToken: ct);
    }

    private static NotificationDto ToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Body = n.Body,
        NotificationType = n.NotificationType,
        TargetId = n.TargetId,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        CreatedAt = n.CreatedAt,
    };
}

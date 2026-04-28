using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Worker.Jobs;

/// <summary>읽은 알림 자동 정리 Job - 90일 이전 읽은 알림 삭제</summary>
public class NotificationCleanupJob
{
    private readonly GroupwareDbContext _db;
    private readonly ILogger<NotificationCleanupJob> _logger;

    public NotificationCleanupJob(GroupwareDbContext db, ILogger<NotificationCleanupJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[NotificationCleanupJob] 오래된 알림 정리 시작");

        var threshold = DateTime.UtcNow.AddDays(-90);
        var result = await _db.Notifications.DeleteManyAsync(
            n => n.IsRead && n.CreatedAt <= threshold, ct);

        _logger.LogInformation("[NotificationCleanupJob] 정리된 알림 {Count}건", result.DeletedCount);
    }
}

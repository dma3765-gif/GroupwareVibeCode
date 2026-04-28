using Engine.Application.Common.Interfaces;
using Engine.Domain.Common.Enums;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Worker.Jobs;

/// <summary>결재 지연 알림 Job - 결재 대기 중인 문서를 확인하여 지연 알림 발행</summary>
public class ApprovalReminderJob
{
    private readonly GroupwareDbContext _db;
    private readonly INotificationPublisher _notifier;
    private readonly ILogger<ApprovalReminderJob> _logger;

    public ApprovalReminderJob(GroupwareDbContext db, INotificationPublisher notifier, ILogger<ApprovalReminderJob> logger)
    {
        _db = db;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[ApprovalReminderJob] 결재 지연 알림 처리 시작");

        var reminderThreshold = DateTime.UtcNow.AddHours(-24); // 24시간 이상 미처리

        var pendingDocs = await _db.ApprovalDocuments
            .Find(d => (d.Status == ApprovalDocumentStatus.InApproval || d.Status == ApprovalDocumentStatus.InAgreement)
                       && d.UpdatedAt <= reminderThreshold && !d.IsDeleted)
            .ToListAsync(ct);

        _logger.LogInformation("[ApprovalReminderJob] 지연 문서 {Count}건 발견", pendingDocs.Count);

        foreach (var doc in pendingDocs)
        {
            // 현재 결재 대기 중인 결재자 찾기
            var pendingApprover = doc.ApprovalLine
                .Where(l => l.Status == ApproverStatus.Pending &&
                            l.Role is ApprovalLineRole.Approval or ApprovalLineRole.Agreement)
                .OrderBy(l => l.Seq)
                .FirstOrDefault();

            if (pendingApprover == null) continue;

            try
            {
                await _notifier.PublishAsync(
                    pendingApprover.UserId,
                    NotificationType.SystemNotice,
                    "결재 처리 지연 알림",
                    $"'{doc.Title}' 문서의 결재가 24시간 이상 대기 중입니다. 확인 부탁드립니다.",
                    "ApprovalDocument",
                    doc.Id,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ApprovalReminderJob] 알림 발행 실패 - DocId: {DocId}", doc.Id);
            }
        }

        _logger.LogInformation("[ApprovalReminderJob] 처리 완료");
    }
}

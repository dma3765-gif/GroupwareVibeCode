using Engine.Domain.Attendance;
using Engine.Domain.Common.Enums;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Worker.Jobs;

/// <summary>근태 자동 마감 Job - 당일 퇴근 미처리건 자동 처리</summary>
public class AttendanceAutoCloseJob
{
    private readonly GroupwareDbContext _db;
    private readonly ILogger<AttendanceAutoCloseJob> _logger;

    public AttendanceAutoCloseJob(GroupwareDbContext db, ILogger<AttendanceAutoCloseJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[AttendanceAutoCloseJob] 근태 자동 마감 시작");

        // 어제 날짜 기준 미퇴근 처리
        var yesterdayStart = DateTime.UtcNow.AddDays(-1).Date;
        var yesterdayEnd = yesterdayStart.AddDays(1);

        var notClosed = await _db.AttendanceRecords
            .Find(a => a.WorkDate >= yesterdayStart
                && a.WorkDate < yesterdayEnd
                && a.CheckInTime != null
                && a.CheckOutTime == null
                && !a.IsDeleted)
            .ToListAsync(ct);

        _logger.LogInformation("[AttendanceAutoCloseJob] 미퇴근 처리 대상 {Count}건", notClosed.Count);

        foreach (var record in notClosed)
        {
            // 자정 기준 퇴근 처리
            var autoCheckOut = record.WorkDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            record.CheckOutTime = autoCheckOut;
            record.WorkMinutes = (int)(autoCheckOut - record.CheckInTime!.Value).TotalMinutes;
            record.Status = AttendanceStatus.Normal;
            record.Remark = "자동 퇴근 처리";
            record.UpdatedAt = DateTime.UtcNow;

            await _db.AttendanceRecords.ReplaceOneAsync(a => a.Id == record.Id, record, cancellationToken: ct);
        }

        _logger.LogInformation("[AttendanceAutoCloseJob] 자동 마감 완료");
    }
}

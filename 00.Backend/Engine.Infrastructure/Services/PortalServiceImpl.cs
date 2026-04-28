using Engine.Application.Approval;
using Engine.Application.Attendance;
using Engine.Application.Calendar;
using Engine.Application.Common.Interfaces;
using Engine.Application.Portal;
using Engine.Domain.Approval;
using Engine.Domain.Attendance;
using Engine.Domain.Board;
using Engine.Domain.Calendar;
using Engine.Domain.Common.Enums;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

public class PortalServiceImpl : IPortalService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public PortalServiceImpl(GroupwareDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PortalDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var pendingTask = _db.ApprovalDocuments.CountDocumentsAsync(
            d => !d.IsDeleted
              && d.Status == ApprovalDocumentStatus.InApproval
              && d.ApprovalLine.Any(l => l.UserId == userId && l.Status == ApproverStatus.Pending),
            cancellationToken: ct);

        var noticesTask = _db.BoardPosts
            .Find(p => p.IsNotice && !p.IsDeleted && !p.IsHidden)
            .SortByDescending(p => p.CreatedAt)
            .Limit(5)
            .ToListAsync(ct);

        var attendanceTask = _db.AttendanceRecords
            .Find(r => r.UserId == userId && r.WorkDate == today && !r.IsDeleted)
            .FirstOrDefaultAsync(ct);

        var leaveBalanceTask = _db.LeaveBalances
            .Find(b => b.UserId == userId && b.Year == today.Year && !b.IsDeleted)
            .FirstOrDefaultAsync(ct);

        var eventsTask = _db.CalendarEvents
            .Find(e => !e.IsDeleted && e.StartAt >= today && e.StartAt < today.AddDays(1)
                && (e.CreatedBy == userId || e.AttendeeIds.Contains(userId)))
            .SortBy(e => e.StartAt)
            .Limit(5)
            .ToListAsync(ct);

        var unreadCountTask = _db.Notifications.CountDocumentsAsync(
            n => n.ReceiverId == userId && !n.IsRead && !n.IsDeleted,
            cancellationToken: ct);

        await Task.WhenAll(pendingTask, noticesTask, attendanceTask, leaveBalanceTask, eventsTask, unreadCountTask);

        var attendance = await attendanceTask;
        var leaveBalance = await leaveBalanceTask;

        return new PortalDashboardDto
        {
            PendingApprovalCount = (int)await pendingTask,
            RecentNotices = (await noticesTask).Select(p => new NoticeSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                CreatedAt = p.CreatedAt,
            }).ToList(),
            TodayAttendance = attendance == null ? null : new AttendanceSummaryDto
            {
                CheckInTime = attendance.CheckInTime,
                CheckOutTime = attendance.CheckOutTime,
                Status = attendance.Status.ToString(),
            },
            LeaveBalance = leaveBalance == null ? null : new LeaveBalanceSummaryDto
            {
                TotalDays = leaveBalance.TotalDays,
                UsedDays = leaveBalance.UsedDays,
                RemainingDays = leaveBalance.RemainingDays,
            },
            TodayEvents = (await eventsTask).Select(e => new EventSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                StartAt = e.StartAt,
                EndAt = e.EndAt,
                IsAllDay = e.IsAllDay,
            }).ToList(),
            UnreadNotificationCount = (int)await unreadCountTask,
        };
    }

    public async Task<List<WebpartSetting>> GetWebpartsAsync(CancellationToken ct = default)
    {
        // 사용자별 포털 웹파트 설정 조회 (기본값 반환)
        return
        [
            new WebpartSetting { Id = "pending_approval", Name = "결재 대기", IsVisible = true, SortOrder = 1 },
            new WebpartSetting { Id = "notice", Name = "공지사항", IsVisible = true, SortOrder = 2 },
            new WebpartSetting { Id = "attendance", Name = "근태 현황", IsVisible = true, SortOrder = 3 },
            new WebpartSetting { Id = "calendar", Name = "오늘의 일정", IsVisible = true, SortOrder = 4 },
            new WebpartSetting { Id = "leave_balance", Name = "연차 현황", IsVisible = true, SortOrder = 5 },
        ];
    }

    public async Task SaveWebpartsAsync(List<WebpartSetting> webparts, CancellationToken ct = default)
    {
        // 사용자별 웹파트 설정 저장 (SystemSettings 컬렉션 활용)
        await Task.CompletedTask;
    }
}

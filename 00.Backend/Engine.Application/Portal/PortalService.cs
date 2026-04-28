namespace Engine.Application.Portal;

// ─── Portal Dashboard DTOs ───

public class PortalDashboardDto
{
    public int PendingApprovalCount { get; set; }
    public List<NoticeSummaryDto> RecentNotices { get; set; } = new();
    public AttendanceSummaryDto? TodayAttendance { get; set; }
    public LeaveBalanceSummaryDto? LeaveBalance { get; set; }
    public List<EventSummaryDto> TodayEvents { get; set; } = new();
    public int UnreadNotificationCount { get; set; }
}

public class NoticeSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AttendanceSummaryDto
{
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class LeaveBalanceSummaryDto
{
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
}

public class EventSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsAllDay { get; set; }
}

// ─── Webpart Settings ───

public class WebpartSetting
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public int SortOrder { get; set; }
}

// ─── Service Interface ───

public interface IPortalService
{
    Task<PortalDashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<List<WebpartSetting>> GetWebpartsAsync(CancellationToken ct = default);
    Task SaveWebpartsAsync(List<WebpartSetting> webparts, CancellationToken ct = default);
}


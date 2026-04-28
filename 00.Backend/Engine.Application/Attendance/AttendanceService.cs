using Engine.Application.Common.DTOs;
using Engine.Application.Common.Responses;
using Engine.Domain.Common.Enums;

namespace Engine.Application.Attendance;

// ─── DTOs ───

public class AttendanceRecordDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string WorkType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int WorkMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string? Location { get; set; }
    public string? Memo { get; set; }
}

public class CheckInRequest
{
    public string? Location { get; set; }
    public WorkType WorkType { get; set; } = WorkType.Office;
}

public class CheckOutRequest
{
    public string? Memo { get; set; }
}

public class AttendanceQuery : PagedRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public AttendanceStatus? Status { get; set; }
}

public class LeaveBalanceDto
{
    public string? Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
}

public class LeaveRequest
{
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    public string? HalfDayPeriod { get; set; }
    public string? Reason { get; set; }
}

public class LeaveRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DaysCount { get; set; }
    public string? Reason { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LeaveApproveRequest
{
    public bool IsApproved { get; set; }
    public string? Comment { get; set; }
}

// ─── Service Interface ───

public interface IAttendanceService
{
    Task<AttendanceRecordDto> CheckInAsync(CheckInRequest request, CancellationToken ct = default);
    Task<AttendanceRecordDto> CheckOutAsync(CheckOutRequest request, CancellationToken ct = default);
    Task<AttendanceRecordDto?> GetTodayRecordAsync(CancellationToken ct = default);
    Task<PagedResult<AttendanceRecordDto>> GetMyRecordsAsync(AttendanceQuery query, CancellationToken ct = default);
    Task<PagedResult<AttendanceRecordDto>> GetTeamRecordsAsync(string departmentId, AttendanceQuery query, CancellationToken ct = default);
    Task<LeaveBalanceDto> GetMyLeaveBalanceAsync(int? year = null, CancellationToken ct = default);
    Task<LeaveRequestDto> ApplyLeaveAsync(LeaveRequest request, CancellationToken ct = default);
    Task<LeaveRequestDto> ApproveLeaveAsync(string requestId, LeaveApproveRequest request, CancellationToken ct = default);
}


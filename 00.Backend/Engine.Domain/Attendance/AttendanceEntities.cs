using Engine.Domain.Common.Entities;
using Engine.Domain.Common.Enums;

namespace Engine.Domain.Attendance;

/// <summary>근태 기록</summary>
public class AttendanceRecord : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    /// <summary>체크인 시간 (CheckInAt 별칭)</summary>
    public DateTime? CheckInTime { get => CheckInAt; set => CheckInAt = value; }
    /// <summary>체크아웃 시간 (CheckOutAt 별칭)</summary>
    public DateTime? CheckOutTime { get => CheckOutAt; set => CheckOutAt = value; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Normal;
    public WorkType WorkType { get; set; } = WorkType.Office;
    public string? CheckInIp { get; set; }
    public string? CheckOutIp { get; set; }
    public string? Location { get; set; }
    public int WorkMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string? ApprovalDocumentId { get; set; } // 연장근무신청서 ID
    public string? Remark { get; set; }
    /// <summary>메모 (Remark 별칭)</summary>
    public string? Memo { get => Remark; set => Remark = value; }
    public bool IsManuallyFixed { get; set; } = false;
    public string? FixedBy { get; set; }
}

/// <summary>휴가 잔여량</summary>
public class LeaveBalance : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; }
    public LeaveType LeaveType { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays => TotalDays - UsedDays;
    public List<LeaveBalanceHistory> Histories { get; set; } = new();
}

public class LeaveBalanceHistory
{
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public decimal Delta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ApprovalDocumentId { get; set; }
}

/// <summary>휴가 신청</summary>
public class LeaveRequest : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    public string? HalfDayPeriod { get; set; } // AM / PM
    public decimal Days { get; set; }
    public decimal DaysCount { get => Days; set => Days = value; }
    public string? Reason { get; set; }
    public LeaveApprovalStatus ApprovalStatus { get; set; } = LeaveApprovalStatus.Pending;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalComment { get; set; }
    public string? ApprovalDocumentId { get; set; }
}

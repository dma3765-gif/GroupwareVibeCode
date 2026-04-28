using Engine.Domain.Common.Entities;
using Engine.Domain.Common.Enums;

namespace Engine.Domain.Calendar;

/// <summary>캘린더 (개인/부서/전사)</summary>
public class Calendar : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#4A90E2";
    public string CalendarType { get; set; } = "Personal"; // Personal, Department, Company, Shared
    public string? OwnerUserId { get; set; }
    /// <summary>소유자 ID 별칭 (OwnerUserId)</summary>
    public string? OwnerId { get => OwnerUserId; set => OwnerUserId = value; }
    public string? DepartmentId { get; set; }
    public bool IsPublic { get; set; } = true;
    /// <summary>공유 대상 사용자 ID 목록</summary>
    public List<string> SharedUserIds { get; set; } = new();
}

/// <summary>일정 이벤트</summary>
public class CalendarEvent : BaseEntity
{
    public string CalendarId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsAllDay { get; set; } = false;
    public bool IsRecurring { get; set; } = false;
    public string? RecurrenceRule { get; set; } // iCal RRULE
    public string OrganizerUserId { get; set; } = string.Empty;
    public List<EventAttendee> Attendees { get; set; } = new();
    /// <summary>참석자 ID 목록 (빠른 조회용)</summary>
    public List<string> AttendeeIds { get; set; } = new();
    /// <summary>이벤트 색상</summary>
    public string? Color { get; set; }
    public string? ApprovalDocumentId { get; set; } // 결재 문서 연동
    public string? ResourceId { get; set; }         // 자원예약 연동
    public string? ReminderMinutes { get; set; }
}

public class EventAttendee
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Declined
}

/// <summary>자원 (회의실, 차량, 장비)</summary>
public class Resource : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ResourceType ResourceType { get; set; }
    public string? Location { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresApproval { get; set; } = false;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}

/// <summary>자원 예약</summary>
public class ResourceReservation : BaseEntity
{
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string ReserverId { get; set; } = string.Empty;
    public string ReserverName { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public int? AttendeeCount { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Confirmed"; // Confirmed, Pending, Cancelled
    /// <summary>취소 여부 (Status == Cancelled 편의 속성)</summary>
    public bool IsCanceled { get => Status == "Cancelled"; set { if (value) Status = "Cancelled"; } }
    public string? CalendarEventId { get; set; }
}

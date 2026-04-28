using Engine.Application.Common.DTOs;
using Engine.Application.Common.Responses;
using Engine.Domain.Common.Enums;

namespace Engine.Application.Calendar;

// ─── Calendar DTOs ───

public class CalendarDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsPublic { get; set; }
    public string? OwnerId { get; set; }
}

public class CalendarEventDto
{
    public string Id { get; set; } = string.Empty;
    public string CalendarId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Color { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public List<string> AttendeeIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CalendarEventQuery
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<string>? CalendarIds { get; set; }
}

public class CreateEventRequest
{
    public string CalendarId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Color { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsAllDay { get; set; } = false;
    public string? RecurrenceRule { get; set; }
    public List<string> AttendeeIds { get; set; } = new();
    public int? ReminderMinutes { get; set; }
}

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Color { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsAllDay { get; set; }
    public string? RecurrenceRule { get; set; }
    public List<string> AttendeeIds { get; set; } = new();
}

// ─── Resource DTOs ───

public class ResourceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; }
}

public class ResourceReservationDto
{
    public string Id { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ReserverId { get; set; } = string.Empty;
    public string ReserverName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}

public class CreateReservationRequest
{
    public string ResourceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int AttendeeCount { get; set; }
    public string? Note { get; set; }
}

// ─── Service Interfaces ───

public interface ICalendarService
{
    Task<List<CalendarDto>> GetMyCalendarsAsync(CancellationToken ct = default);
    Task<List<CalendarEventDto>> GetEventsAsync(CalendarEventQuery query, CancellationToken ct = default);
    Task<CalendarEventDto> GetEventByIdAsync(string id, CancellationToken ct = default);
    Task<CalendarEventDto> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default);
    Task<CalendarEventDto> UpdateEventAsync(string id, UpdateEventRequest request, CancellationToken ct = default);
    Task DeleteEventAsync(string id, CancellationToken ct = default);
}

public interface IResourceReservationService
{
    Task<List<ResourceDto>> GetResourcesAsync(string resourceType = "", CancellationToken ct = default);
    Task<List<ResourceReservationDto>> GetReservationsAsync(string resourceId, DateTime date, CancellationToken ct = default);
    Task<ResourceReservationDto> ReserveAsync(CreateReservationRequest request, CancellationToken ct = default);
    Task CancelReservationAsync(string id, CancellationToken ct = default);
}


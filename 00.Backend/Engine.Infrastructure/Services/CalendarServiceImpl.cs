using Engine.Application.Calendar;
using Engine.Application.Common.DTOs;
using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Domain.Calendar;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

public class CalendarServiceImpl : ICalendarService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogService _audit;

    public CalendarServiceImpl(GroupwareDbContext db, ICurrentUserContext currentUser, IAuditLogService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<List<CalendarDto>> GetMyCalendarsAsync(CancellationToken ct = default)
    {
        var cals = await _db.Calendars
            .Find(c => !c.IsDeleted && (c.OwnerId == _currentUser.UserId || c.SharedUserIds.Contains(_currentUser.UserId) || c.IsPublic))
            .ToListAsync(ct);
        return cals.Select(ToDto).ToList();
    }

    public async Task<List<CalendarEventDto>> GetEventsAsync(CalendarEventQuery query, CancellationToken ct = default)
    {
        var filter = Builders<CalendarEvent>.Filter.And(
            Builders<CalendarEvent>.Filter.Eq(e => e.IsDeleted, false),
            Builders<CalendarEvent>.Filter.Lte(e => e.StartAt, query.End),
            Builders<CalendarEvent>.Filter.Gte(e => e.EndAt, query.Start)
        );

        if (query.CalendarIds?.Any() == true)
            filter &= Builders<CalendarEvent>.Filter.In(e => e.CalendarId, query.CalendarIds);
        else
        {
            // 내 캘린더 기본 조회
            var myCalIds = (await _db.Calendars
                .Find(c => !c.IsDeleted && (c.OwnerId == _currentUser.UserId || c.SharedUserIds.Contains(_currentUser.UserId) || c.IsPublic))
                .Project(c => c.Id).ToListAsync(ct));
            filter &= Builders<CalendarEvent>.Filter.In(e => e.CalendarId, myCalIds);
        }

        var events = await _db.CalendarEvents.Find(filter).SortBy(e => e.StartAt).ToListAsync(ct);
        return events.Select(ToEventDto).ToList();
    }

    public async Task<CalendarEventDto> GetEventByIdAsync(string eventId, CancellationToken ct = default)
    {
        var ev = await _db.CalendarEvents.Find(e => e.Id == eventId && !e.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("CalendarEvent", eventId);
        return ToEventDto(ev);
    }

    public async Task<CalendarEventDto> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        // 캘린더 접근 권한 확인
        var cal = await _db.Calendars.Find(c => c.Id == request.CalendarId && !c.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Calendar", request.CalendarId);
        if (cal.OwnerId != _currentUser.UserId && !cal.SharedUserIds.Contains(_currentUser.UserId))
            throw new ForbiddenException("해당 캘린더에 일정을 추가할 권한이 없습니다.");

        var ev = new CalendarEvent
        {
            CalendarId = request.CalendarId,
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            IsAllDay = request.IsAllDay,
            Location = request.Location,
            Color = request.Color,
            IsRecurring = !string.IsNullOrEmpty(request.RecurrenceRule),
            RecurrenceRule = request.RecurrenceRule,
            AttendeeIds = request.AttendeeIds,
            ReminderMinutes = request.ReminderMinutes?.ToString(),
            CreatedBy = _currentUser.UserId,
        };

        await _db.CalendarEvents.InsertOneAsync(ev, cancellationToken: ct);
        await _audit.LogAsync("CALENDAR_EVENT_CREATE", "CalendarEvent", ev.Id, ct: ct);
        return ToEventDto(ev);
    }

    public async Task<CalendarEventDto> UpdateEventAsync(string eventId, UpdateEventRequest request, CancellationToken ct = default)
    {
        var ev = await _db.CalendarEvents.Find(e => e.Id == eventId && !e.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("CalendarEvent", eventId);

        var cal = await _db.Calendars.Find(c => c.Id == ev.CalendarId).FirstOrDefaultAsync(ct)!;
        if (cal.OwnerId != _currentUser.UserId && !cal.SharedUserIds.Contains(_currentUser.UserId))
            throw new ForbiddenException("수정 권한이 없습니다.");

        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.StartAt = request.StartAt;
        ev.EndAt = request.EndAt;
        ev.IsAllDay = request.IsAllDay;
        ev.Location = request.Location;
        ev.Color = request.Color;
        ev.RecurrenceRule = request.RecurrenceRule;
        ev.AttendeeIds = request.AttendeeIds;
        ev.UpdatedAt = DateTime.UtcNow;
        ev.UpdatedBy = _currentUser.UserId;

        await _db.CalendarEvents.ReplaceOneAsync(e => e.Id == eventId, ev, cancellationToken: ct);
        return ToEventDto(ev);
    }

    public async Task DeleteEventAsync(string eventId, CancellationToken ct = default)
    {
        var ev = await _db.CalendarEvents.Find(e => e.Id == eventId && !e.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("CalendarEvent", eventId);

        await _db.CalendarEvents.UpdateOneAsync(e => e.Id == eventId,
            Builders<CalendarEvent>.Update.Set(e => e.IsDeleted, true).Set(e => e.DeletedAt, DateTime.UtcNow),
            cancellationToken: ct);
    }

    private static CalendarDto ToDto(Calendar c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Color = c.Color,
        IsPublic = c.IsPublic,
        OwnerId = c.OwnerId,
    };

    private static CalendarEventDto ToEventDto(CalendarEvent e) => new()
    {
        Id = e.Id,
        CalendarId = e.CalendarId,
        Title = e.Title,
        Description = e.Description,
        StartAt = e.StartAt,
        EndAt = e.EndAt,
        IsAllDay = e.IsAllDay,
        Location = e.Location,
        Color = e.Color,
        IsRecurring = e.IsRecurring,
        RecurrenceRule = e.RecurrenceRule,
        AttendeeIds = e.AttendeeIds,
        CreatedAt = e.CreatedAt,
    };
}

public class ResourceReservationServiceImpl : IResourceReservationService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public ResourceReservationServiceImpl(GroupwareDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ResourceDto>> GetResourcesAsync(string resourceType, CancellationToken ct = default)
    {
        var resources = await _db.Resources.Find(r => !r.IsDeleted && r.IsActive).ToListAsync(ct);
        return resources.Select(r => new ResourceDto
        {
            Id = r.Id,
            Name = r.Name,
            ResourceType = r.ResourceType.ToString(),
            Capacity = r.Capacity,
            Location = r.Location,
            Description = r.Description,
        }).ToList();
    }

    public async Task<List<ResourceReservationDto>> GetReservationsAsync(string resourceId, DateTime date, CancellationToken ct = default)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        var reservations = await _db.ResourceReservations
            .Find(r => r.ResourceId == resourceId && r.StartAt >= start && r.StartAt < end && !r.IsDeleted && r.IsCanceled == false)
            .ToListAsync(ct);

        return reservations.Select(r => new ResourceReservationDto
        {
            Id = r.Id,
            ResourceId = r.ResourceId,
            Title = r.Title,
            StartAt = r.StartAt,
            EndAt = r.EndAt,
            ReserverId = r.ReserverId,
            ReserverName = r.ReserverName,
        }).ToList();
    }

    public async Task<ResourceReservationDto> ReserveAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        // 중복 예약 확인
        var conflicts = await _db.ResourceReservations.CountDocumentsAsync(
            r => r.ResourceId == request.ResourceId
              && !r.IsDeleted && !r.IsCanceled
              && r.StartAt < request.EndAt && r.EndAt > request.StartAt,
            cancellationToken: ct);

        if (conflicts > 0)
            throw new DomainException("해당 시간에 이미 예약이 있습니다.");

        var reservation = new ResourceReservation
        {
            ResourceId = request.ResourceId,
            Title = request.Title,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            ReserverId = _currentUser.UserId,
            ReserverName = _currentUser.Name,
            DepartmentId = _currentUser.DepartmentId,
            AttendeeCount = request.AttendeeCount,
            Note = request.Note,
            CreatedBy = _currentUser.UserId,
        };

        await _db.ResourceReservations.InsertOneAsync(reservation, cancellationToken: ct);
        return new ResourceReservationDto
        {
            Id = reservation.Id,
            ResourceId = reservation.ResourceId,
            Title = reservation.Title,
            StartAt = reservation.StartAt,
            EndAt = reservation.EndAt,
            ReserverId = reservation.ReserverId,
            ReserverName = reservation.ReserverName,
        };
    }

    public async Task CancelReservationAsync(string reservationId, CancellationToken ct = default)
    {
        var r = await _db.ResourceReservations.Find(r => r.Id == reservationId && !r.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ResourceReservation", reservationId);
        if (r.ReserverId != _currentUser.UserId)
            throw new ForbiddenException("예약 취소 권한이 없습니다.");

        await _db.ResourceReservations.UpdateOneAsync(r => r.Id == reservationId,
            Builders<ResourceReservation>.Update.Set(r => r.IsCanceled, true).Set(r => r.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);
    }
}

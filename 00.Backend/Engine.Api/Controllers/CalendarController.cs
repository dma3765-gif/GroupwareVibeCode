using Engine.Application.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class CalendarController : BaseController
{
    private readonly ICalendarService _calendar;
    private readonly IResourceReservationService _resource;

    public CalendarController(ICalendarService calendar, IResourceReservationService resource)
    {
        _calendar = calendar;
        _resource = resource;
    }

    /// <summary>내 캘린더 목록</summary>
    [HttpGet("calendars")]
    public async Task<IActionResult> GetCalendars(CancellationToken ct)
        => Ok(await _calendar.GetMyCalendarsAsync(ct));

    /// <summary>일정 조회</summary>
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] CalendarEventQuery query, CancellationToken ct)
        => Ok(await _calendar.GetEventsAsync(query, ct));

    /// <summary>일정 상세</summary>
    [HttpGet("events/{id}")]
    public async Task<IActionResult> GetEvent(string id, CancellationToken ct)
        => Ok(await _calendar.GetEventByIdAsync(id, ct));

    /// <summary>일정 생성</summary>
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request, CancellationToken ct)
        => Created(await _calendar.CreateEventAsync(request, ct));

    /// <summary>일정 수정</summary>
    [HttpPut("events/{id}")]
    public async Task<IActionResult> UpdateEvent(string id, [FromBody] UpdateEventRequest request, CancellationToken ct)
        => Ok(await _calendar.UpdateEventAsync(id, request, ct));

    /// <summary>일정 삭제</summary>
    [HttpDelete("events/{id}")]
    public async Task<IActionResult> DeleteEvent(string id, CancellationToken ct)
    {
        await _calendar.DeleteEventAsync(id, ct);
        return NoContent();
    }

    // ── 자원 예약 ──────────────────────────────────────────────

    /// <summary>자원 목록 조회</summary>
    [HttpGet("resources")]
    public async Task<IActionResult> GetResources([FromQuery] string resourceType = "", CancellationToken ct = default)
        => Ok(await _resource.GetResourcesAsync(resourceType, ct));

    /// <summary>자원 예약 현황</summary>
    [HttpGet("resources/{resourceId}/reservations")]
    public async Task<IActionResult> GetReservations(string resourceId, [FromQuery] DateTime date, CancellationToken ct)
        => Ok(await _resource.GetReservationsAsync(resourceId, date, ct));

    /// <summary>자원 예약</summary>
    [HttpPost("resources/reservations")]
    public async Task<IActionResult> Reserve([FromBody] CreateReservationRequest request, CancellationToken ct)
        => Created(await _resource.ReserveAsync(request, ct));

    /// <summary>예약 취소</summary>
    [HttpDelete("resources/reservations/{id}")]
    public async Task<IActionResult> CancelReservation(string id, CancellationToken ct)
    {
        await _resource.CancelReservationAsync(id, ct);
        return NoContent();
    }
}

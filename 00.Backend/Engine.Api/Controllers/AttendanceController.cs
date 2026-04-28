using Engine.Application.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class AttendanceController : BaseController
{
    private readonly IAttendanceService _service;
    public AttendanceController(IAttendanceService service) => _service = service;

    /// <summary>출근 체크인</summary>
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken ct)
        => Ok(await _service.CheckInAsync(request, ct));

    /// <summary>퇴근 체크아웃</summary>
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request, CancellationToken ct)
        => Ok(await _service.CheckOutAsync(request, ct));

    /// <summary>오늘 근태 조회</summary>
    [HttpGet("today")]
    public async Task<IActionResult> Today(CancellationToken ct)
        => Ok(await _service.GetTodayRecordAsync(ct));

    /// <summary>내 근태 내역</summary>
    [HttpGet("my")]
    public async Task<IActionResult> MyRecords([FromQuery] AttendanceQuery query, CancellationToken ct)
        => Ok(await _service.GetMyRecordsAsync(query, ct));

    /// <summary>팀 근태 내역 (부서장)</summary>
    [HttpGet("team/{departmentId}")]
    public async Task<IActionResult> TeamRecords(string departmentId, [FromQuery] AttendanceQuery query, CancellationToken ct)
        => Ok(await _service.GetTeamRecordsAsync(departmentId, query, ct));

    /// <summary>연차 잔여 조회</summary>
    [HttpGet("leave/balance")]
    public async Task<IActionResult> LeaveBalance([FromQuery] int? year, CancellationToken ct)
        => Ok(await _service.GetMyLeaveBalanceAsync(year, ct));

    /// <summary>휴가 신청</summary>
    [HttpPost("leave")]
    public async Task<IActionResult> ApplyLeave([FromBody] LeaveRequest request, CancellationToken ct)
        => Created(await _service.ApplyLeaveAsync(request, ct));

    /// <summary>휴가 승인/반려</summary>
    [HttpPost("leave/{requestId}/approve")]
    public async Task<IActionResult> ApproveLeave(string requestId, [FromBody] LeaveApproveRequest request, CancellationToken ct)
        => Ok(await _service.ApproveLeaveAsync(requestId, request, ct));
}

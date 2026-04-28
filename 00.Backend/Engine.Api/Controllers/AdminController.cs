using Engine.Application.Admin;
using Engine.Application.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

/// <summary>관리자용 API</summary>
[Authorize(Roles = "SystemAdmin,GroupwareAdmin")]
[Route("api/admin")]
public class AdminController : BaseController
{
    private readonly ISystemCodeService _codeSvc;
    private readonly IMenuService _menuSvc;
    private readonly ISystemSettingService _settingSvc;
    private readonly IAuditLogQueryService _auditSvc;

    public AdminController(
        ISystemCodeService codeSvc,
        IMenuService menuSvc,
        ISystemSettingService settingSvc,
        IAuditLogQueryService auditSvc)
    {
        _codeSvc = codeSvc;
        _menuSvc = menuSvc;
        _settingSvc = settingSvc;
        _auditSvc = auditSvc;
    }

    // ─── 시스템 코드 관리 ───

    /// <summary>코드 목록 조회</summary>
    [HttpGet("codes")]
    public async Task<IActionResult> GetCodes([FromQuery] string? groupCode, [FromQuery] PagedRequest request, CancellationToken ct)
        => Ok(await _codeSvc.GetCodesAsync(groupCode, request, ct));

    /// <summary>그룹 코드 전체 조회 (사용 여부: 활성)</summary>
    [HttpGet("codes/group/{groupCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCodesByGroup(string groupCode, CancellationToken ct)
        => Ok(await _codeSvc.GetCodesByGroupAsync(groupCode, ct));

    /// <summary>코드 단건 조회</summary>
    [HttpGet("codes/{id}")]
    public async Task<IActionResult> GetCode(string id, CancellationToken ct)
        => Ok(await _codeSvc.GetCodeByIdAsync(id, ct));

    /// <summary>코드 생성/수정</summary>
    [HttpPost("codes")]
    public async Task<IActionResult> UpsertCode([FromBody] UpsertSystemCodeRequest request, CancellationToken ct)
        => Ok(await _codeSvc.UpsertCodeAsync(request, ct));

    /// <summary>코드 삭제</summary>
    [HttpDelete("codes/{id}")]
    public async Task<IActionResult> DeleteCode(string id, CancellationToken ct)
    {
        await _codeSvc.DeleteCodeAsync(id, ct);
        return Ok();
    }

    // ─── 메뉴 관리 ───

    /// <summary>전체 메뉴 트리 조회</summary>
    [HttpGet("menus")]
    public async Task<IActionResult> GetMenuTree(CancellationToken ct)
        => Ok(await _menuSvc.GetMenuTreeAsync(ct));

    /// <summary>내 권한 기반 메뉴 조회</summary>
    [HttpGet("menus/my")]
    [AllowAnonymous]
    [Authorize]
    public async Task<IActionResult> GetMyMenus(CancellationToken ct)
        => Ok(await _menuSvc.GetMyMenusAsync(ct));

    /// <summary>메뉴 단건 조회</summary>
    [HttpGet("menus/{id}")]
    public async Task<IActionResult> GetMenu(string id, CancellationToken ct)
        => Ok(await _menuSvc.GetMenuByIdAsync(id, ct));

    /// <summary>메뉴 생성</summary>
    [HttpPost("menus")]
    public async Task<IActionResult> CreateMenu([FromBody] UpsertMenuRequest request, CancellationToken ct)
        => Created(await _menuSvc.CreateMenuAsync(request, ct));

    /// <summary>메뉴 수정</summary>
    [HttpPut("menus/{id}")]
    public async Task<IActionResult> UpdateMenu(string id, [FromBody] UpsertMenuRequest request, CancellationToken ct)
        => Ok(await _menuSvc.UpdateMenuAsync(id, request, ct));

    /// <summary>메뉴 삭제</summary>
    [HttpDelete("menus/{id}")]
    public async Task<IActionResult> DeleteMenu(string id, CancellationToken ct)
    {
        await _menuSvc.DeleteMenuAsync(id, ct);
        return Ok();
    }

    // ─── 시스템 설정 ───

    /// <summary>시스템 설정 목록</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings([FromQuery] string? category, CancellationToken ct)
        => Ok(await _settingSvc.GetAllSettingsAsync(category, ct));

    /// <summary>시스템 설정 키별 조회</summary>
    [HttpGet("settings/{key}")]
    public async Task<IActionResult> GetSetting(string key, CancellationToken ct)
        => Ok(await _settingSvc.GetSettingByKeyAsync(key, ct));

    /// <summary>시스템 설정 수정</summary>
    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSystemSettingRequest request, CancellationToken ct)
        => Ok(await _settingSvc.UpdateSettingAsync(key, request, ct));

    // ─── 감사로그 ───

    /// <summary>감사로그 목록 조회</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQuery query, CancellationToken ct)
        => Ok(await _auditSvc.GetLogsAsync(query, ct));
}

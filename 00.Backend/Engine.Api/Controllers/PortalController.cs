using Engine.Application.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class PortalController : BaseController
{
    private readonly IPortalService _service;
    public PortalController(IPortalService service) => _service = service;

    /// <summary>포털 대시보드</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
        => Ok(await _service.GetDashboardAsync(ct));

    /// <summary>웹파트 설정 조회</summary>
    [HttpGet("webparts")]
    public async Task<IActionResult> GetWebparts(CancellationToken ct)
        => Ok(await _service.GetWebpartsAsync(ct));

    /// <summary>웹파트 설정 저장</summary>
    [HttpPut("webparts")]
    public async Task<IActionResult> SaveWebparts([FromBody] List<WebpartSetting> settings, CancellationToken ct)
    {
        await _service.SaveWebpartsAsync(settings, ct);
        return NoContent("설정이 저장되었습니다.");
    }
}

using Engine.Application.Approval;
using Engine.Application.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class ApprovalFormsController : BaseController
{
    private readonly IApprovalFormService _service;
    public ApprovalFormsController(IApprovalFormService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, CancellationToken ct)
        => Ok(await _service.GetFormsAsync(request, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
        => Ok(await _service.GetFormByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,ApprovalAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateApprovalFormRequest request, CancellationToken ct)
        => Created(await _service.CreateFormAsync(request, ct));

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,ApprovalAdmin")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateApprovalFormRequest request, CancellationToken ct)
        => Ok(await _service.UpdateFormAsync(id, request, ct));

    [HttpDelete("{id}")]
    [Authorize(Roles = "SystemAdmin,ApprovalAdmin")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteFormAsync(id, ct);
        return NoContent();
    }
}

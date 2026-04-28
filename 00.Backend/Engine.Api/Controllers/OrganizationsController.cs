using Engine.Application.Common.DTOs;
using Engine.Application.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class OrganizationsController : BaseController
{
    private readonly IOrganizationService _orgService;
    public OrganizationsController(IOrganizationService orgService) => _orgService = orgService;

    /// <summary>조직도 트리 조회</summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree(CancellationToken ct)
        => Ok(await _orgService.GetTreeAsync(ct));

    /// <summary>부서 목록 조회</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _orgService.GetTreeAsync(ct));

    /// <summary>부서 상세 조회</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
        => Ok(await _orgService.GetByIdAsync(id, ct));

    /// <summary>부서 생성</summary>
    [HttpPost]
    [Authorize(Roles = "SystemAdmin,OrgAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request, CancellationToken ct)
        => Created(await _orgService.CreateAsync(request, ct));

    /// <summary>부서 수정</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,OrgAdmin")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
        => Ok(await _orgService.UpdateAsync(id, request, ct));

    /// <summary>부서 삭제</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SystemAdmin,OrgAdmin")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _orgService.DeleteAsync(id, ct);
        return NoContent("부서가 삭제되었습니다.");
    }
}

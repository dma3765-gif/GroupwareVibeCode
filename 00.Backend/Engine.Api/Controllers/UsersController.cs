using Engine.Application.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class UsersController : BaseController
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>사용자 검색</summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] UserSearchRequest request, CancellationToken ct)
        => Ok(await _userService.SearchAsync(request, ct));

    /// <summary>사용자 상세 조회</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
        => Ok(await _userService.GetByIdAsync(id, ct));

    /// <summary>사용자 생성</summary>
    [HttpPost]
    [Authorize(Roles = "SystemAdmin,OrgAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
        => Created(await _userService.CreateAsync(request, ct));

    /// <summary>사용자 수정</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,OrgAdmin")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        => Ok(await _userService.UpdateAsync(id, request, ct));

    /// <summary>사용자 삭제</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SystemAdmin,OrgAdmin")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _userService.DeactivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>역할 할당</summary>
    [HttpPost("{id}/roles")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        await _userService.AssignRoleAsync(id, request.RoleName, ct);
        return NoContent("역할이 할당되었습니다.");
    }

    /// <summary>역할 해제</summary>
    [HttpDelete("{id}/roles/{roleName}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> RemoveRole(string id, string roleName, CancellationToken ct)
    {
        await _userService.RemoveRoleAsync(id, roleName, ct);
        return NoContent("역할이 해제되었습니다.");
    }
}

public record AssignRoleRequest(string RoleName);

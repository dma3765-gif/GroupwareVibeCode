using Engine.Application.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult Ok<T>(T data, string? message = null) =>
        base.Ok(ApiResponse<T>.Ok(data, message ?? "처리되었습니다."));

    protected IActionResult Created<T>(T data, string? message = null) =>
        base.StatusCode(201, ApiResponse<T>.Ok(data, message ?? "생성되었습니다."));

    protected IActionResult NoContent(string? message = null) =>
        base.Ok(new ApiResponse { Success = true, Code = "OK", Message = message ?? "처리되었습니다." });
}

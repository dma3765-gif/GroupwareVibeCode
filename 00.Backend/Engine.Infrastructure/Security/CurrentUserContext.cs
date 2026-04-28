using System.Security.Claims;
using Engine.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Engine.Infrastructure.Security;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub") ?? string.Empty;

    public string Name => Principal?.FindFirstValue("name") ?? string.Empty;

    public string DepartmentId => Principal?.FindFirstValue("departmentId") ?? string.Empty;

    public string DepartmentName => Principal?.FindFirstValue("departmentName") ?? string.Empty;

    public string PositionName => Principal?.FindFirstValue("positionName") ?? string.Empty;

    public string TenantId => Principal?.FindFirstValue("tenantId") ?? "default";

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public bool HasPermission(string permission)
        => Principal?.Claims.Any(c => c.Type == "permission" && c.Value == permission) ?? false;
}

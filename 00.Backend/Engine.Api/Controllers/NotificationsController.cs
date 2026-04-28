using Engine.Application.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Api.Controllers;

[Authorize]
public class NotificationsController : BaseController
{
    private readonly INotificationService _service;
    public NotificationsController(INotificationService service) => _service = service;

    /// <summary>알림 목록</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] NotificationQuery query, CancellationToken ct)
        => Ok(await _service.GetMyNotificationsAsync(query, ct));

    /// <summary>읽지 않은 알림 수</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
        => Ok(await _service.GetUnreadCountAsync(ct));

    /// <summary>알림 읽음 처리</summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(string id, CancellationToken ct)
    {
        await _service.MarkAsReadAsync(id, ct);
        return NoContent();
    }

    /// <summary>전체 읽음 처리</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _service.MarkAllAsReadAsync(ct);
        return NoContent("모두 읽음 처리되었습니다.");
    }

    /// <summary>알림 삭제</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteNotificationAsync(id, ct);
        return NoContent();
    }
}

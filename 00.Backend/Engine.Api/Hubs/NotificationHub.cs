using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Engine.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>클라이언트에서 특정 그룹(부서) 구독</summary>
    public async Task JoinDepartment(string departmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dept_{departmentId}");
    }

    public async Task LeaveDepartment(string departmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dept_{departmentId}");
    }

    /// <summary>클라이언트에서 읽음 처리 요청</summary>
    public async Task MarkRead(string notificationId)
    {
        // 클라이언트가 직접 REST API를 호출하도록 유도하는 방식이 더 안전하지만
        // 편의상 Hub에서도 처리 가능하도록 이벤트를 발행
        await Clients.Caller.SendAsync("NotificationRead", notificationId);
    }
}

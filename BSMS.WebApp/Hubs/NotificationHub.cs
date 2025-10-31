using Microsoft.AspNetCore.SignalR;

namespace BSMS.WebApp.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications in Battery Swap Management System
/// </summary>
public class NotificationHub : Hub
{
    /// <summary>
    /// Send notification to specific user
    /// </summary>
    public async Task SendNotificationToUser(string userId, string message, string type = "info")
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send notification to all users with specific role
    /// </summary>
    public async Task SendNotificationToRole(string role, string message, string type = "info")
    {
        await Clients.Group(role).SendAsync("ReceiveNotification", new
        {
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast notification to all connected clients
    /// </summary>
    public async Task BroadcastNotification(string message, string type = "info")
    {
        await Clients.All.SendAsync("ReceiveNotification", new
        {
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update battery status in real-time
    /// </summary>
    public async Task UpdateBatteryStatus(string batteryId, string status, int soh)
    {
        await Clients.All.SendAsync("BatteryStatusUpdated", new
        {
            batteryId,
            status,
            soh,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify about new swap transaction
    /// </summary>
    public async Task NotifySwapTransaction(string stationId, string userId, string status)
    {
        await Clients.Group($"Station_{stationId}").SendAsync("SwapTransactionUpdate", new
        {
            stationId,
            userId,
            status,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Join station-specific group for staff
    /// </summary>
    public async Task JoinStationGroup(string stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Station_{stationId}");
    }

    /// <summary>
    /// Leave station group
    /// </summary>
    public async Task LeaveStationGroup(string stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Station_{stationId}");
    }

    public override async Task OnConnectedAsync()
    {
        // Get user claims and add to role group
        var httpContext = Context.GetHttpContext();
        if (httpContext?.User.Identity?.IsAuthenticated ?? false)
        {
            var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

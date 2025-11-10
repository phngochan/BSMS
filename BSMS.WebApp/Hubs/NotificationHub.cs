using BSMS.BLL.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BSMS.WebApp.Hubs;

public class NotificationHub : Hub
{
    private readonly IServiceProvider _serviceProvider;

    public NotificationHub(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task SendNotificationToUser(string userId, string message, string type = "info")
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendNotificationToRole(string role, string message, string type = "info")
    {
        await Clients.Group(role).SendAsync("ReceiveNotification", new
        {
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task BroadcastNotification(string message, string type = "info")
    {
        await Clients.All.SendAsync("ReceiveNotification", new
        {
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

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

    public async Task JoinStationGroup(string stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Station_{stationId}");
    }

    public async Task LeaveStationGroup(string stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Station_{stationId}");
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext?.User.Identity?.IsAuthenticated ?? false)
        {
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
            }

            if (role == "StationStaff")
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    var user = await userService.GetUserWithVehiclesAsync(userId);
                    var staffStation = user?.StationStaffs?.FirstOrDefault();
                    if (staffStation?.StationId != null)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"Station_{staffStation.StationId}");
                    }
                }
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

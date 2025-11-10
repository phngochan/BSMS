using BSMS.BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Background;

public class ReservationAutoCancelService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationAutoCancelService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public ReservationAutoCancelService(
        IServiceProvider serviceProvider,
        ILogger<ReservationAutoCancelService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationAutoCancelService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reservationService = scope.ServiceProvider
                    .GetRequiredService<IReservationService>();
                var reservationRepo = scope.ServiceProvider
                    .GetRequiredService<BSMS.DAL.Repositories.IReservationRepository>();
                var batteryService = scope.ServiceProvider
                    .GetRequiredService<BSMS.BLL.Services.IBatteryService>();

                var lateReservations = await reservationRepo.GetLateReservationsAsync();
                int cancelledCount = 0;
                var cancelledUserIds = new HashSet<int>();

                foreach (var reservation in lateReservations)
                {
                    await reservationRepo.UpdateStatusAsync(reservation.ReservationId, BSMS.BusinessObjects.Enums.ReservationStatus.Cancelled);

                    if (reservation.BatteryId.HasValue)
                    {
                        await batteryService.UpdateBatteryStatusAsync(reservation.BatteryId.Value, BSMS.BusinessObjects.Enums.BatteryStatus.Full);
                        _logger.LogInformation("Battery status reset to Full after auto-cancel: BatteryId={BatteryId}", 
                            reservation.BatteryId.Value);
                    }

                    cancelledUserIds.Add(reservation.UserId);
                    cancelledCount++;

                    _logger.LogInformation("Auto-cancelled late reservation: {ReservationId}, UserId: {UserId}",
                        reservation.ReservationId, reservation.UserId);
                }

                if (cancelledCount > 0)
                {
                    _logger.LogInformation("Auto-cancelled {Count} late reservation(s)", cancelledCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while auto-cancelling late reservations");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("ReservationAutoCancelService stopped");
    }
}


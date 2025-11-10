using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IStationService _stationService;
    private readonly IReservationService _reservationService;

    public IndexModel(
        ILogger<IndexModel> logger,
        IStationService stationService,
        IReservationService reservationService)
    {
        _logger = logger;
        _stationService = stationService;
        _reservationService = reservationService;
    }

    public List<StationHighlightViewModel> StationHighlights { get; private set; } = new();
    public ReservationSummaryViewModel? ActiveReservation { get; private set; }
    public List<ReservationSummaryViewModel> RecentReservations { get; private set; } = new();
    public int TotalAvailableBatteries { get; private set; }
    public DateTime? LastCompletedSwap { get; private set; }
    public int ActiveReservationSecondsRemaining { get; private set; }
    public string BatteryStatusNote { get; private set; } = "Dữ liệu pin chi tiết sẽ được cập nhật sau.";

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Driver dashboard accessed without valid user id");
            return;
        }

        await LoadStationHighlightsAsync();
        await LoadReservationsAsync(userId);
    }

    private async Task LoadStationHighlightsAsync()
    {
        var stations = await _stationService.GetStationsWithAvailabilityAsync();

        StationHighlights = stations
            .OrderByDescending(s => s.AvailableBatteries)
            .ThenBy(s => s.Name)
            .Take(3)
            .Select(s => new StationHighlightViewModel
            {
                StationId = s.StationId,
                Name = s.Name,
                Address = s.Address,
                AvailableBatteries = s.AvailableBatteries,
                Capacity = s.Capacity,
                Latitude = s.Latitude,
                Longitude = s.Longitude
            })
            .ToList();

        TotalAvailableBatteries = StationHighlights.Sum(s => s.AvailableBatteries);

        if (!StationHighlights.Any())
        {
            BatteryStatusNote = "Hiện chưa có trạm nào hoạt động.";
        }
    }

    private async Task LoadReservationsAsync(int userId)
    {
        var activeReservation = await _reservationService.GetActiveReservationAsync(userId);
        if (activeReservation != null)
        {
            ActiveReservation = new ReservationSummaryViewModel
            {
                ReservationId = activeReservation.ReservationId,
                StationId = activeReservation.StationId,
                StationName = activeReservation.Station?.Name ?? "Chưa xác định",
                StationAddress = activeReservation.Station?.Address ?? string.Empty,
                ScheduledTime = activeReservation.ScheduledTime,
                Status = activeReservation.Status.ToString()
            };

            var seconds = (int)Math.Round((activeReservation.ScheduledTime - DateTime.UtcNow).TotalSeconds);
            ActiveReservationSecondsRemaining = Math.Max(0, seconds);
        }

        var reservations = await _reservationService.GetMyReservationsAsync(userId, pageNumber: 1, pageSize: 10);

        RecentReservations = reservations
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new ReservationSummaryViewModel
            {
                ReservationId = r.ReservationId,
                StationId = r.StationId,
                StationName = r.Station?.Name ?? "Chưa xác định",
                StationAddress = r.Station?.Address ?? string.Empty,
                ScheduledTime = r.ScheduledTime,
                Status = r.Status.ToString()
            })
            .ToList();

        LastCompletedSwap = reservations
            .Where(r => r.Status == ReservationStatus.Completed)
            .OrderByDescending(r => r.ScheduledTime)
            .Select(r => (DateTime?)r.ScheduledTime)
            .FirstOrDefault();

        if (LastCompletedSwap.HasValue)
        {
            BatteryStatusNote = $"Lần đổi pin gần nhất: {LastCompletedSwap.Value:dd/MM/yyyy HH:mm}";
        }
    }
}

public class StationHighlightViewModel
{
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int AvailableBatteries { get; set; }
    public int Capacity { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ReservationSummaryViewModel
{
    public int ReservationId { get; set; }
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

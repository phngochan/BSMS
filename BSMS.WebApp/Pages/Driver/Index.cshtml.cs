using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.ViewModels.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IStationService _stationService;
    private readonly IReservationService _reservationService;
    private readonly IUserService _userService;

    public IndexModel(
        ILogger<IndexModel> logger,
        IStationService stationService,
        IReservationService reservationService,
        IUserService userService)
    {
        _logger = logger;
        _stationService = stationService;
        _reservationService = reservationService;
        _userService = userService;
    }

    public List<StationHighlightViewModel> StationHighlights { get; private set; } = new();
    public ReservationSummaryViewModel? ActiveReservation { get; private set; }
    public List<ReservationSummaryViewModel> RecentReservations { get; private set; } = new();
    public List<SwapHistoryViewModel> RecentSwaps { get; private set; } = new();
    public List<Vehicle> UserVehicles { get; private set; } = new();
    public int TotalAvailableBatteries { get; private set; }
    public DateTime? LastCompletedSwap { get; private set; }
    public int ActiveReservationSecondsRemaining { get; private set; }
    public string BatteryStatusNote { get; private set; } = "Dữ liệu pin chi tiết sẽ được cập nhật sau.";
    public BatteryStatusViewModel? BatteryStatus { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedVehicleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public double? UserLatitude { get; set; }

    [BindProperty(SupportsGet = true)]
    public double? UserLongitude { get; set; }

    public async Task OnGetAsync(int? vehicleId = null, double? lat = null, double? lng = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Driver dashboard accessed without valid user id");
            return;
        }

        await LoadUserVehiclesAsync(userId);

        if (UserVehicles.Count > 1 && !vehicleId.HasValue)
        {
            SelectedVehicleId = null;
            BatteryStatusNote = "Vui lòng chọn vehicle để xem battery status.";
        }
        else
        {
            SelectedVehicleId = vehicleId;
            if (UserVehicles.Count == 1 && !vehicleId.HasValue)
            {
                SelectedVehicleId = UserVehicles.First().VehicleId;
            }
        }

        UserLatitude = lat;
        UserLongitude = lng;

        await LoadStationHighlightsAsync();
        await LoadReservationsAsync(userId);
        await LoadSwapHistoryAsync(userId);
        
        if (SelectedVehicleId.HasValue)
        {
            await LoadBatteryStatusAsync(userId, SelectedVehicleId);
        }
    }

    private async Task LoadStationHighlightsAsync()
    {
        var stations = await _stationService.GetStationsWithAvailabilityAsync();

        var highlights = stations
            .Select(s => new StationHighlightViewModel
            {
                StationId = s.StationId,
                Name = s.Name,
                Address = s.Address,
                AvailableBatteries = s.AvailableBatteries,
                Capacity = s.Capacity,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Distance = UserLatitude.HasValue && UserLongitude.HasValue
                    ? CalculateDistance(UserLatitude.Value, UserLongitude.Value, s.Latitude, s.Longitude)
                    : null
            })
            .ToList();

        StationHighlights = highlights
            .OrderBy(s => s.Distance ?? double.MaxValue)
            .ThenByDescending(s => s.AvailableBatteries)
            .ThenBy(s => s.Name)
            .Take(3)
            .ToList();

        TotalAvailableBatteries = StationHighlights.Sum(s => s.AvailableBatteries);

        if (!StationHighlights.Any())
        {
            BatteryStatusNote = "Hiện chưa có trạm nào hoạt động.";
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
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

    private async Task LoadUserVehiclesAsync(int userId)
    {
        var user = await _userService.GetUserWithVehiclesAsync(userId);
        if (user?.Vehicles != null)
        {
            UserVehicles = user.Vehicles.ToList();
        }
    }

    private async Task LoadSwapHistoryAsync(int userId)
    {
        var user = await _userService.GetUserWithTransactionsAsync(userId);
        if (user?.SwapTransactions != null)
        {
            RecentSwaps = user.SwapTransactions
                .Where(st => st.Status == SwapStatus.Completed)
                .OrderByDescending(st => st.SwapTime)
                .Take(5)
                .Select(st => new SwapHistoryViewModel
                {
                    TransactionId = st.TransactionId,
                    SwapTime = st.SwapTime,
                    StationName = st.Station?.Name ?? "Unknown",
                    BatteryTakenModel = st.BatteryTaken?.Model ?? "N/A",
                    BatteryReturnedModel = st.BatteryReturned?.Model ?? "N/A",
                    TotalCost = st.TotalCost,
                    Duration = "N/A"
                })
                .ToList();
        }
    }

    private async Task LoadBatteryStatusAsync(int userId, int? vehicleId = null)
    {
        var user = await _userService.GetUserWithTransactionsAsync(userId);
        if (user?.SwapTransactions != null)
        {
            var query = user.SwapTransactions
                .Where(st => st.Status == SwapStatus.Completed);

            if (vehicleId.HasValue)
            {
                query = query.Where(st => st.VehicleId == vehicleId.Value);
            }

            var lastSwap = query
                .OrderByDescending(st => st.SwapTime)
                .FirstOrDefault();

            if (lastSwap != null && lastSwap.BatteryTaken != null)
            {
                var battery = lastSwap.BatteryTaken;
                var daysSinceSwap = (DateTime.UtcNow - lastSwap.SwapTime).TotalDays;

                var degradationPerDay = 0.1m;
                var degradation = (decimal)daysSinceSwap * degradationPerDay;
                var currentSoh = battery.Soh - degradation;
                currentSoh = Math.Max(0, currentSoh);

                var soh = (int)Math.Round(currentSoh);
                var estimatedRange = (int)Math.Round(soh * 1.7);

                BatteryStatus = new BatteryStatusViewModel
                {
                    Percent = soh,
                    EstimatedRange = estimatedRange,
                    LastSwapped = lastSwap.SwapTime
                };

                LastCompletedSwap = lastSwap.SwapTime;
            }
        }
    }
}

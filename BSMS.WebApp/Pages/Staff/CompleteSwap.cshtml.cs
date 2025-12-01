using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BSMS.WebApp.Pages.Staff;

[Authorize(Roles = "StationStaff,Admin")]
public class CompleteSwapModel : BasePageModel
{
    private readonly IReservationService _reservationService;
    private readonly ISwapTransactionService _swapTransactionService;
    private readonly IBatteryService _batteryService;
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<CompleteSwapModel> _logger;

    public CompleteSwapModel(
        IReservationService reservationService,
        ISwapTransactionService swapTransactionService,
        IBatteryService batteryService,
        IUserService userService,
        IHubContext<NotificationHub> hubContext,
        ILogger<CompleteSwapModel> logger,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _reservationService = reservationService;
        _swapTransactionService = swapTransactionService;
        _batteryService = batteryService;
        _userService = userService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Reservation? Reservation { get; set; }
    public List<Battery> AvailableBatteries { get; set; } = new();
    public Battery? CurrentBattery { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public int? BatteryTakenId { get; set; }

    [BindProperty]
    public int? BatteryReturnedId { get; set; }

    [BindProperty]
    public string ReturnedBatteryStatus { get; set; } = "Charging";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Reservation = await _reservationService.GetReservationDetailsAsync(id);
        if (Reservation == null)
        {
            ErrorMessage = "Không tìm thấy đặt chỗ.";
            return RedirectToPage("/Staff/ConfirmReservation");
        }

        if (Reservation.Status != ReservationStatus.Active)
        {
            ErrorMessage = $"Đặt chỗ không còn hoạt động. Trạng thái hiện tại: {Reservation.Status}";
            return RedirectToPage("/Staff/ConfirmReservation", new { id });
        }

        // Lấy pin hiện tại của user (nếu có)
        var user = await _userService.GetUserWithTransactionsAsync(Reservation.UserId);
        if (user?.SwapTransactions != null)
        {
            var lastSwap = user.SwapTransactions
                .Where(st => st.Status == SwapStatus.Completed)
                .OrderByDescending(st => st.SwapTime)
                .FirstOrDefault();

            if (lastSwap?.BatteryTaken != null)
            {
                CurrentBattery = lastSwap.BatteryTaken;
                BatteryReturnedId = CurrentBattery.BatteryId;
            }
        }

        // Lấy pin đã được book cho reservation này
        if (Reservation.BatteryId.HasValue)
        {
            var bookedBattery = await _batteryService.GetBatteryDetailsAsync(Reservation.BatteryId.Value);
            if (bookedBattery != null && bookedBattery.Status == BatteryStatus.Booked)
            {
                AvailableBatteries.Add(bookedBattery);
                // Tự động chọn pin đã được book
                BatteryTakenId = bookedBattery.BatteryId;
            }
            else
            {
                ErrorMessage = "Pin đã được đặt chỗ không tồn tại hoặc không còn trạng thái Booked.";
            }
        }
        else
        {
            ErrorMessage = "Đặt chỗ này chưa có pin được book.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Reservation = await _reservationService.GetReservationDetailsAsync(id);
        if (Reservation == null)
        {
            ErrorMessage = "Không tìm thấy đặt chỗ.";
            return Page();
        }

        if (Reservation.Status != ReservationStatus.Active)
        {
            ErrorMessage = $"Đặt chỗ không còn hoạt động. Trạng thái hiện tại: {Reservation.Status}";
            return Page();
        }

        if (!BatteryTakenId.HasValue)
        {
            ModelState.AddModelError("BatteryTakenId", "Vui lòng chọn pin giao.");
        }

        if (!BatteryReturnedId.HasValue)
        {
            ModelState.AddModelError("BatteryReturnedId", "Vui lòng chọn pin nhận.");
        }

        if (!ModelState.IsValid)
        {
            // Reload pin đã được book
            if (Reservation.BatteryId.HasValue)
            {
                var bookedBattery = await _batteryService.GetBatteryDetailsAsync(Reservation.BatteryId.Value);
                if (bookedBattery != null)
                {
                    AvailableBatteries.Clear();
                    AvailableBatteries.Add(bookedBattery);
                }
            }

            var user = await _userService.GetUserWithTransactionsAsync(Reservation.UserId);
            if (user?.SwapTransactions != null)
            {
                var lastSwap = user.SwapTransactions
                    .Where(st => st.Status == SwapStatus.Completed)
                    .OrderByDescending(st => st.SwapTime)
                    .FirstOrDefault();

                if (lastSwap?.BatteryTaken != null)
                {
                    CurrentBattery = lastSwap.BatteryTaken;
                }
            }

            return Page();
        }

        try
        {
            // Validate pin giao phải là Booked và phải match với reservation
            var batteryTaken = await _batteryService.GetBatteryDetailsAsync(BatteryTakenId.Value);
            if (batteryTaken == null)
            {
                ModelState.AddModelError("BatteryTakenId", "Pin giao không tồn tại.");
                if (Reservation.BatteryId.HasValue)
                {
                    var bookedBattery = await _batteryService.GetBatteryDetailsAsync(Reservation.BatteryId.Value);
                    if (bookedBattery != null)
                    {
                        AvailableBatteries.Clear();
                        AvailableBatteries.Add(bookedBattery);
                    }
                }
                return Page();
            }

            if (batteryTaken.Status != BatteryStatus.Booked)
            {
                ModelState.AddModelError("BatteryTakenId", $"Pin giao phải có trạng thái Booked. Pin hiện có trạng thái {batteryTaken.Status}.");
                if (Reservation.BatteryId.HasValue)
                {
                    var bookedBattery = await _batteryService.GetBatteryDetailsAsync(Reservation.BatteryId.Value);
                    if (bookedBattery != null)
                    {
                        AvailableBatteries.Clear();
                        AvailableBatteries.Add(bookedBattery);
                    }
                }
                return Page();
            }

            // Validate pin giao phải match với reservation
            if (Reservation.BatteryId.HasValue && BatteryTakenId.Value != Reservation.BatteryId.Value)
            {
                ModelState.AddModelError("BatteryTakenId", "Pin giao phải là pin đã được đặt chỗ cho reservation này.");
                if (Reservation.BatteryId.HasValue)
                {
                    var bookedBattery = await _batteryService.GetBatteryDetailsAsync(Reservation.BatteryId.Value);
                    if (bookedBattery != null)
                    {
                        AvailableBatteries.Clear();
                        AvailableBatteries.Add(bookedBattery);
                    }
                }
                return Page();
            }

            // Validate pin nhận tồn tại
            var batteryReturned = await _batteryService.GetBatteryDetailsAsync(BatteryReturnedId.Value);
            if (batteryReturned == null)
            {
                ModelState.AddModelError("BatteryReturnedId", "Pin nhận không tồn tại.");
                if (Reservation.BatteryId.HasValue)
                {
                    var bookedBattery = await _batteryService.GetBatteryDetailsAsync(Reservation.BatteryId.Value);
                    if (bookedBattery != null)
                    {
                        AvailableBatteries.Clear();
                        AvailableBatteries.Add(bookedBattery);
                    }
                }
                return Page();
            }

            // Validate và convert trạng thái pin cũ
            BatteryStatus returnedBatteryStatus;
            if (ReturnedBatteryStatus == "Defective")
            {
                returnedBatteryStatus = BatteryStatus.Defective;
            }
            else
            {
                returnedBatteryStatus = BatteryStatus.Charging;
            }

            var totalCost = 0m;
            var transaction = await _swapTransactionService.CompleteSwapTransactionAsync(
                Reservation.UserId,
                Reservation.VehicleId,
                Reservation.StationId,
                BatteryTakenId.Value,
                BatteryReturnedId.Value,
                totalCost,
                returnedBatteryStatus);

            // Complete reservation
            await _reservationService.CompleteReservationAsync(id);

            await LogActivityAsync("COMPLETE_SWAP",
                $"Đã hoàn thành đổi pin cho đặt chỗ #{id}: Pin giao {BatteryTakenId.Value}, Pin nhận {BatteryReturnedId.Value}");

            try
            {
                // Gửi notification cho user
                await _hubContext.Clients.User(Reservation.UserId.ToString()).SendAsync("CompleteReservation", new
                {
                    reservationId = id,
                    message = $"Đặt chỗ tại {Reservation.Station?.Name ?? "trạm"} đã được nhân viên hoàn thành",
                    type = "success",
                    timestamp = DateTime.UtcNow,
                    stationName = Reservation.Station?.Name ?? "trạm"
                });

                // Gửi notification cho nhân viên trạm
                await _hubContext.Clients.Group($"Station_{Reservation.StationId}").SendAsync("CompleteReservation", new
                {
                    reservationId = id,
                    message = $"Đặt chỗ #{id} đã hoàn thành",
                    type = "info",
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("SignalR notification sent for completed swap transaction {TransactionId}", transaction.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification for completed swap transaction {TransactionId}", transaction.TransactionId);
            }

            TempData["SuccessMessage"] = "Đã hoàn thành đổi pin thành công!";
            return RedirectToPage("/Staff/ConfirmReservation");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            if (Reservation.BatteryId.HasValue)
            {
                var bookedBattery = await _batteryService.GetBatteryDetailsAsync(Reservation.BatteryId.Value);
                if (bookedBattery != null)
                {
                    AvailableBatteries.Clear();
                    AvailableBatteries.Add(bookedBattery);
                }
            }
            return Page();
        }
    }
}


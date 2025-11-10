using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSMS.WebApp.Pages.Staff;

[Authorize(Roles = "StationStaff,Admin")]
public class ConfirmReservationModel : PageModel
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ConfirmReservationModel> _logger;

    public ConfirmReservationModel(
        IReservationService reservationService,
        ILogger<ConfirmReservationModel> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    public Reservation? Reservation { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public bool IsConfirmed { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
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
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var result = await _reservationService.ConfirmReservationAsync(id);

        if (result)
        {
            SuccessMessage = "Đã xác nhận đặt chỗ thành công!";
            IsConfirmed = true;
        }
        else
        {
            ErrorMessage = "Không thể xác nhận đặt chỗ. Vui lòng kiểm tra lại trạng thái đặt chỗ.";
        }

        Reservation = await _reservationService.GetReservationDetailsAsync(id);
        return Page();
    }
}


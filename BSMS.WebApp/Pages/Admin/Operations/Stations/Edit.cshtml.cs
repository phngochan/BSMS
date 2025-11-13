using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BSMS.WebApp.Pages.Admin.Operations.Stations;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IChangingStationService _stationService;

    public EditModel(IChangingStationService stationService)
    {
        _stationService = stationService;
    }

    [BindProperty]
    public StationFormModel StationForm { get; set; } = new();

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public SelectList StationStatusOptions { get; set; } = new(Enum.GetValues(typeof(StationStatus)));

    public async Task<IActionResult> OnGetAsync(int? stationId, string? returnUrl)
    {
        ReturnUrl = returnUrl;

        if (stationId.HasValue && stationId.Value > 0)
        {
            var station = await _stationService.GetStationAsync(stationId.Value);
            if (station == null)
            {
                return NotFound();
            }

            StationForm = new StationFormModel
            {
                StationId = station.StationId,
                Name = station.Name,
                Address = station.Address,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                Capacity = station.Capacity,
                Status = station.Status
            };
        }
        else
        {
            StationForm = new StationFormModel
            {
                Latitude = 10.8144,
                Longitude = 106.7102,
                Capacity = 20,
                Status = StationStatus.Active
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // ✅ CUSTOM VALIDATION: Round coordinates to 6 decimal places
        StationForm.Latitude = Math.Round(StationForm.Latitude, 6);
        StationForm.Longitude = Math.Round(StationForm.Longitude, 6);

        // ✅ Revalidate after rounding
        ModelState.Clear();
        if (!TryValidateModel(StationForm, nameof(StationForm)))
        {
            return Page();
        }

        try
        {
            var station = new ChangingStation
            {
                StationId = StationForm.StationId,
                Name = StationForm.Name,
                Address = StationForm.Address,
                Latitude = StationForm.Latitude,
                Longitude = StationForm.Longitude,
                Capacity = StationForm.Capacity,
                Status = StationForm.Status
            };

            if (StationForm.StationId == 0)
            {
                station.CreatedAt = DateTime.UtcNow;
                await _stationService.CreateStationAsync(station);
                TempData["SuccessMessage"] = $"Đã tạo trạm {station.Name} thành công!";
            }
            else
            {
                await _stationService.UpdateStationAsync(station);
                TempData["SuccessMessage"] = $"Đã cập nhật trạm {station.Name} thành công!";
            }

            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Admin/Operations/Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi: {ex.Message}");
            return Page();
        }
    }

    public class StationFormModel
    {
        public int StationId { get; set; }

        [Required(ErrorMessage = "Tên trạm không được để trống")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vĩ độ không được để trống")]
        [Range(-90, 90, ErrorMessage = "Vĩ độ phải từ -90 đến 90")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống")]
        [Range(-180, 180, ErrorMessage = "Kinh độ phải từ -180 đến 180")]
        public double Longitude { get; set; }

        [Required(ErrorMessage = "Sức chứa không được để trống")]
        [Range(1, 1000, ErrorMessage = "Sức chứa phải từ 1 đến 1000")]
        public int Capacity { get; set; }

        [Required]
        public StationStatus Status { get; set; }
    }
}

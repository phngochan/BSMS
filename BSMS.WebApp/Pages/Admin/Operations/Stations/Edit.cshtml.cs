using System.ComponentModel.DataAnnotations;
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BSMS.WebApp.Pages.Admin.Operations.Stations;

[Authorize(Roles = "Admin")]
public class EditModel : BasePageModel
{
    private readonly IChangingStationService _stationService;
    private readonly IStationStaffService _stationStaffService;
    private readonly IBatteryService _batteryService;

    public EditModel(
        IChangingStationService stationService,
        IStationStaffService stationStaffService,
        IBatteryService batteryService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _stationService = stationService;
        _stationStaffService = stationStaffService;
        _batteryService = batteryService;
    }

    [BindProperty(SupportsGet = true)]
    public int? StationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public StationInput StationForm { get; set; } = new();

    public IEnumerable<SelectListItem> StationStatusOptions =>
        Enum.GetValues<StationStatus>()
            .Cast<StationStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public async Task<IActionResult> OnGetAsync(int? stationId)
    {
        StationId = stationId;
        ReturnUrl ??= Url.Page("/Admin/Operations/Index");
        StationForm = new StationInput { Status = StationStatus.Active };
        if (stationId.HasValue)
        {
            var station = await _stationService.GetStationAsync(stationId.Value);
            if (station == null)
            {
                return NotFound();
            }

            StationForm = new StationInput
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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        ReturnUrl = returnUrl ?? ReturnUrl ?? Url.Page("/Admin/Operations/Index");
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var validationMessage = await ValidateStationStatusRulesAsync();
        if (!string.IsNullOrEmpty(validationMessage))
        {
            ModelState.AddModelError(string.Empty, validationMessage);
            return Page();
        }

        try
        {
            if (StationForm.StationId == 0)
            {
                await _stationService.CreateStationAsync(new ChangingStation
                {
                    Name = StationForm.Name,
                    Address = StationForm.Address,
                    Latitude = StationForm.Latitude,
                    Longitude = StationForm.Longitude,
                    Capacity = StationForm.Capacity,
                    Status = StationForm.Status
                });
                TempData["SuccessMessage"] = "Đã tạo trạm mới.";
                await LogActivityAsync("Station", $"Created station {StationForm.Name}");
            }
            else
            {
                await _stationService.UpdateStationAsync(new ChangingStation
                {
                    StationId = StationForm.StationId,
                    Name = StationForm.Name,
                    Address = StationForm.Address,
                    Latitude = StationForm.Latitude,
                    Longitude = StationForm.Longitude,
                    Capacity = StationForm.Capacity,
                    Status = StationForm.Status
                });
                TempData["SuccessMessage"] = "Đã cập nhật thông tin trạm.";
                await LogActivityAsync("Station", $"Updated station #{StationForm.StationId}");
            }

            return LocalRedirect(ReturnUrl ?? Url.Page("/Admin/Operations/Index"));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(ex));
            return Page();
        }
    }

    private string GetFriendlyErrorMessage(Exception ex)
    {
        if (ex is DbUpdateException dbEx)
        {
            var detail = dbEx.InnerException?.Message ?? dbEx.Message;
            if (!string.IsNullOrWhiteSpace(detail) &&
                detail.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            {
                return "Không thể thao tác vì bản ghi đang được tham chiếu ở khu vực khác. Vui lòng xử lý dữ liệu liên quan trước khi thử lại.";
            }

            return detail;
        }

        return ex.Message;
    }

    private async Task<string?> ValidateStationStatusRulesAsync()
    {
        if (StationForm == null || StationForm.StationId == 0)
        {
            return null;
        }

        var assignments = (await _stationStaffService.GetAssignmentsByStationAsync(StationForm.StationId)).ToList();

        if (StationForm.Status == StationStatus.Inactive)
        {
            if (assignments.Any())
            {
                return "Không thể chuyển trạm sang trạng thái Inactive khi vẫn còn nhân viên đang được phân công.";
            }

            var hasBookedBattery = (await _batteryService.GetBatteriesByStationAsync(StationForm.StationId))
                .Any(b => b.Status == BatteryStatus.Booked);
            if (hasBookedBattery)
            {
                return "Không thể chuyển trạm sang trạng thái Inactive khi vẫn còn pin đang ở trạng thái Booked.";
            }
        }

        if (StationForm.Status == StationStatus.Active && !assignments.Any())
        {
            return "Trạm ở trạng thái Active phải có ít nhất một nhân viên.";
        }

        return null;
    }

    // ✅ Nested StationInput class - ĐÃ SỬA LỖI
    public class StationInput : IValidatableObject
    {
        public int StationId { get; set; }  

        [Required(ErrorMessage = "Tên trạm là bắt buộc")]
        [StringLength(150, ErrorMessage = "Tên trạm không được vượt quá 150 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(250, ErrorMessage = "Địa chỉ không được vượt quá 250 ký tự")]
        public string Address { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "Latitude phải nằm trong khoảng -90 đến 90")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude phải nằm trong khoảng -180 đến 180")]
        public double Longitude { get; set; }

        [Range(1, 1000, ErrorMessage = "Sức chứa phải từ 1 đến 1000")]
        public int Capacity { get; set; }

        [Required]
        public StationStatus Status { get; set; } = StationStatus.Active;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validate Latitude có đúng 6 chữ số thập phân
            var latDecimalPlaces = GetDecimalPlaces(Latitude);
            if (latDecimalPlaces > 6)
            {
                yield return new ValidationResult(
                    "Latitude chỉ được phép tối đa 6 chữ số thập phân (ví dụ: 10.844445)",
                    new[] { nameof(Latitude) });
            }

            // Validate Longitude có đúng 6 chữ số thập phân
            var lngDecimalPlaces = GetDecimalPlaces(Longitude);
            if (lngDecimalPlaces > 6)
            {
                yield return new ValidationResult(
                    "Longitude chỉ được phép tối đa 6 chữ số thập phân (ví dụ: 106.715696)",
                    new[] { nameof(Longitude) });
            }
        }

        private static int GetDecimalPlaces(double value)
        {
            var valueString = value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            var decimalIndex = valueString.IndexOf('.');
            if (decimalIndex == -1)
                return 0;
            
            return valueString.Length - decimalIndex - 1;
        }
    }
}

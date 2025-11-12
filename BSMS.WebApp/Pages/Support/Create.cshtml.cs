using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BSMS.WebApp.Pages.DriverSupport;

[Authorize(Roles = "Driver")]
public class CreateModel : BasePageModel
{
    private readonly ISupportService _supportService;
    private readonly IStationService _stationService;

    public CreateModel(
        ISupportService supportService,
        IStationService stationService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _supportService = supportService;
        _stationService = stationService;
    }

    [BindProperty]
    public SupportRequestInput SupportForm { get; set; } = new();

    public List<SelectListItem> StationOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadStationsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadStationsAsync();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var trimmedDescription = (SupportForm.Description ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmedDescription))
        {
            ModelState.AddModelError("SupportForm.Description", "Mô tả là bắt buộc.");
            return Page();
        }

        try
        {
            var support = new Support
            {
                UserId = CurrentUserId,
                StationId = SupportForm.StationId,
                Type = SupportForm.Type,
                Description = trimmedDescription,
                Status = SupportStatus.Open
            };

            await _supportService.CreateSupportAsync(support);
            await LogActivityAsync("Support", $"Driver {CurrentUserId} requested help for station {SupportForm.StationId}");
            TempData["SuccessMessage"] = "Yêu cầu hỗ trợ đã được gửi.";
            return RedirectToPage("/Driver/Index");
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Không thể gửi yêu cầu vào lúc này. Vui lòng thử lại sau.");
            return Page();
        }
    }

    private async Task LoadStationsAsync()
    {
        var stations = await _stationService.GetActiveStationsAsync();
        StationOptions = stations
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem($"{s.Name} - {s.Address}", s.StationId.ToString()))
            .ToList();
    }

    public class SupportRequestInput
    {
        [Required(ErrorMessage = "Vui lòng chọn trạm")]
        public int StationId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại hỗ trợ")]
        public SupportType Type { get; set; } = SupportType.BatteryIssue;

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Mô tả không quá 1000 ký tự")]
        public string Description { get; set; } = string.Empty;
    }
}

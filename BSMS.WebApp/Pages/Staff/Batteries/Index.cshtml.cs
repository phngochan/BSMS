using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BSMS.WebApp.Pages.Staff.Batteries;

[Authorize(Roles = "StationStaff")]
public class IndexModel : BasePageModel
{
    private readonly IStationStaffService _stationStaffService;
    private readonly IBatteryService _batteryService;
    private readonly IChangingStationService _stationService;

    public IndexModel(
        IStationStaffService stationStaffService,
        IBatteryService batteryService,
        IChangingStationService stationService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _stationStaffService = stationStaffService;
        _batteryService = batteryService;
        _stationService = stationService;
    }

    public StationStaff? Assignment { get; private set; }
    public ChangingStation? StationInfo { get; private set; }
    public List<Battery> Batteries { get; private set; } = new();
    public Dictionary<BatteryStatus, int> BatterySummary { get; private set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public BatteryStatus? StatusFilter { get; set; }

    public bool HasStation => Assignment != null;

    // Allowed status transitions for staff
    private static readonly IReadOnlyDictionary<BatteryStatus, BatteryStatus[]> StaffAllowedTransitions = new Dictionary<BatteryStatus, BatteryStatus[]>
    {
        [BatteryStatus.Full] = new[] { BatteryStatus.Charging, BatteryStatus.Defective },
        [BatteryStatus.Charging] = new[] { BatteryStatus.Full, BatteryStatus.Defective },
        [BatteryStatus.Taken] = new[] { BatteryStatus.Defective },
        [BatteryStatus.Booked] = Array.Empty<BatteryStatus>(),
        [BatteryStatus.Defective] = new[] { BatteryStatus.Charging }
    };

    public IEnumerable<SelectListItem> GetSelectableStatuses(BatteryStatus currentStatus)
    {
        var yielded = new HashSet<BatteryStatus> { currentStatus };
        yield return new SelectListItem(currentStatus.ToString(), currentStatus.ToString(), true);

        if (StaffAllowedTransitions.TryGetValue(currentStatus, out var allowed))
        {
            foreach (var status in allowed)
            {
                if (yielded.Add(status))
                {
                    yield return new SelectListItem(status.ToString(), status.ToString());
                }
            }
        }
    }

    public IEnumerable<SelectListItem> StatusFilterOptions =>
        Enum.GetValues<BatteryStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString(), StatusFilter == status));

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadStationDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateBatteryStatusAsync(int batteryId, BatteryStatus status, string? defectReason)
    {
        await LoadStationDataAsync();
        if (!HasStation)
        {
            TempData["ErrorMessage"] = "Bạn chưa được phân công trạm nào.";
            return RedirectToPage();
        }

        var battery = await _batteryService.GetBatteryAsync(batteryId);
        if (battery == null || battery.StationId != Assignment!.StationId)
        {
            TempData["ErrorMessage"] = "Pin không tồn tại hoặc không thuộc trạm của bạn.";
            return RedirectToPage();
        }

        var trimmedReason = defectReason?.Trim();
        if (status == BatteryStatus.Defective && string.IsNullOrWhiteSpace(trimmedReason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do pin bị lỗi.";
            return RedirectToPage();
        }

        if (!IsValidStatusTransition(battery.Status, status))
        {
            TempData["ErrorMessage"] = "Không thể thay đổi trạng thái pin theo cách này.";
            return RedirectToPage();
        }

        // Update battery
        if (status == BatteryStatus.Full && battery.Status == BatteryStatus.Charging)
        {
            battery.LastMaintenance = DateTime.UtcNow;
        }

        battery.Status = status;
        battery.DefectNote = status == BatteryStatus.Defective ? trimmedReason : null;
        battery.UpdatedAt = DateTime.UtcNow;
        
        await _batteryService.UpdateBatteryAsync(battery);
        
        var reasonSuffix = status == BatteryStatus.Defective ? $" (Lý do: {trimmedReason})" : string.Empty;
        await LogActivityAsync("Battery", $"Cập nhật pin #{batteryId} -> {status}{reasonSuffix}");

        TempData["SuccessMessage"] = status == BatteryStatus.Defective
            ? $"Pin đã được đánh dấu lỗi: {trimmedReason}"
            : "Trạng thái pin đã được cập nhật.";

        return RedirectToPage(new { SearchTerm, StatusFilter });
    }

    private async Task LoadStationDataAsync()
    {
        if (CurrentUserId == 0)
            return;

        Assignment = await _stationStaffService.GetAssignmentForUserAsync(CurrentUserId);
        if (Assignment == null)
            return;

        StationInfo = await _stationService.GetStationAsync(Assignment.StationId);
        
        var allBatteries = (await _batteryService.GetBatteriesByStationAsync(Assignment.StationId)).ToList();
        
        // Apply filters
        var filteredBatteries = allBatteries.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower();
            filteredBatteries = filteredBatteries.Where(b => 
                b.BatteryId.ToString().Contains(searchLower) ||
                b.Model.ToLower().Contains(searchLower));
        }
        
        if (StatusFilter.HasValue)
        {
            filteredBatteries = filteredBatteries.Where(b => b.Status == StatusFilter.Value);
        }
        
        Batteries = filteredBatteries.OrderBy(b => b.BatteryId).ToList();
        BatterySummary = await _batteryService.GetStatusSummaryAsync(Assignment.StationId);
    }

    private static bool IsValidStatusTransition(BatteryStatus currentStatus, BatteryStatus nextStatus)
    {
        if (currentStatus == nextStatus)
        {
            return true;
        }

        return StaffAllowedTransitions.TryGetValue(currentStatus, out var allowed) &&
               allowed.Contains(nextStatus);
    }
}



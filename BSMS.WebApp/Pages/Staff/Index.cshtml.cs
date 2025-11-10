using System;
using System.Collections.Generic;
using System.Linq;
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BSMS.WebApp.Pages.Staff;

[Authorize(Roles = "StationStaff")]
public class IndexModel : BasePageModel
{
    private readonly IStationStaffService _stationStaffService;
    private readonly IBatteryService _batteryService;
    private readonly ISupportService _supportService;
    private readonly IChangingStationService _stationService;

    public IndexModel(
        IStationStaffService stationStaffService,
        IBatteryService batteryService,
        ISupportService supportService,
        IChangingStationService stationService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _stationStaffService = stationStaffService;
        _batteryService = batteryService;
        _supportService = supportService;
        _stationService = stationService;
    }

    public StationStaff? Assignment { get; private set; }
    public ChangingStation? StationInfo { get; private set; }

    public List<Battery> Batteries { get; private set; } = new();
    public Dictionary<BatteryStatus, int> BatterySummary { get; private set; } = new();
    public List<Support> SupportTickets { get; private set; } = new();

    public bool HasStation => Assignment != null;

    private static readonly IReadOnlyDictionary<BatteryStatus, BatteryStatus[]> StaffAllowedTransitions = new Dictionary<BatteryStatus, BatteryStatus[]>
    {
        [BatteryStatus.Full] = new[] { BatteryStatus.Charging, BatteryStatus.Defective },
        [BatteryStatus.Charging] = new[] { BatteryStatus.Defective },
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

    public IEnumerable<SelectListItem> SupportStatusOptions =>
        Enum.GetValues<SupportStatus>()
            .Where(status => status != SupportStatus.Open)
            .Select(status => new SelectListItem(status.ToString(), status.ToString()));

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
            TempData["ErrorMessage"] = "You are not assigned to any station.";
            return RedirectToPage();
        }

        var battery = await _batteryService.GetBatteryAsync(batteryId);
        if (battery == null || battery.StationId != Assignment!.StationId)
        {
            TempData["ErrorMessage"] = "Battery not found or not part of your station.";
            return RedirectToPage();
        }

        var trimmedReason = defectReason?.Trim();
        if (status == BatteryStatus.Defective && string.IsNullOrWhiteSpace(trimmedReason))
        {
            TempData["ErrorMessage"] = "Please provide the reason why this battery is defective.";
            return RedirectToPage();
        }

        if (!IsValidStatusTransition(battery.Status, status))
        {
            TempData["ErrorMessage"] = "You cannot change the battery status in that way.";
            return RedirectToPage();
        }

        if (battery.Status == BatteryStatus.Charging && status == BatteryStatus.Full)
        {
            battery.LastMaintenance = DateTime.UtcNow;
        }

        battery.Status = status;
        battery.DefectNote = status == BatteryStatus.Defective ? trimmedReason : null;
        battery.UpdatedAt = DateTime.UtcNow;
        await _batteryService.UpdateBatteryAsync(battery);
        var reasonSuffix = status == BatteryStatus.Defective ? $" (Reason: {trimmedReason})" : string.Empty;
        await LogActivityAsync("Battery", $"Updated battery #{batteryId} -> {status}{reasonSuffix}");

        TempData["SuccessMessage"] = status == BatteryStatus.Defective
            ? $"Battery marked defective: {trimmedReason}"
            : "Battery status updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostProcessSupportAsync(int supportId, SupportStatus status, int? rating)
    {
        await LoadStationDataAsync();
        if (!HasStation)
        {
            TempData["ErrorMessage"] = "You are not assigned to any station.";
            return RedirectToPage();
        }

        var support = await _supportService.GetSupportAsync(supportId);
        if (support == null || support.StationId != Assignment!.StationId)
        {
            TempData["ErrorMessage"] = "This ticket does not belong to your station.";
            return RedirectToPage();
        }

        int? appliedRating = status == SupportStatus.Closed ? rating : null;
        await _supportService.UpdateSupportStatusAsync(supportId, status, appliedRating);
        await LogActivityAsync("Support", $"Updated ticket #{supportId} -> {status}");

        TempData["SuccessMessage"] = "Support ticket updated.";
        return RedirectToPage();
    }

    private async Task LoadStationDataAsync()
    {
        if (CurrentUserId == 0)
            return;

        Assignment = await _stationStaffService.GetAssignmentForUserAsync(CurrentUserId);
        if (Assignment == null)
            return;

        StationInfo = await _stationService.GetStationAsync(Assignment.StationId);
        Batteries = (await _batteryService.GetBatteriesByStationAsync(Assignment.StationId)).ToList();
        BatterySummary = await _batteryService.GetStatusSummaryAsync(Assignment.StationId);
        SupportTickets = (await _supportService.GetSupportsByStationAsync(Assignment.StationId)).ToList();
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

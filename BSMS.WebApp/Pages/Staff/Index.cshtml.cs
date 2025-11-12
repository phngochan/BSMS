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
    private readonly ISwapTransactionService _swapTransactionService;

    public IndexModel(
        IStationStaffService stationStaffService,
        IBatteryService batteryService,
        ISupportService supportService,
        IChangingStationService stationService,
        ISwapTransactionService swapTransactionService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _stationStaffService = stationStaffService;
        _batteryService = batteryService;
        _supportService = supportService;
        _stationService = stationService;
        _swapTransactionService = swapTransactionService;
    }

    public StationStaff? Assignment { get; private set; }
    public ChangingStation? StationInfo { get; private set; }

    public List<Battery> Batteries { get; private set; } = new();
    public Dictionary<BatteryStatus, int> BatterySummary { get; private set; } = new();
    public List<Support> SupportTickets { get; private set; } = new();
    public StaffShiftViewModel? ShiftInfo { get; private set; }
    public SwapWorkflowViewModel SwapWorkflow { get; private set; } = SwapWorkflowViewModel.Empty;
    public SupportSummaryViewModel SupportSummary { get; private set; } = SupportSummaryViewModel.Empty;

    [TempData]
    public string? SupportSuccessMessage { get; set; }

    public bool HasStation => Assignment != null;

    private static readonly IReadOnlyDictionary<BatteryStatus, BatteryStatus[]> StaffAllowedTransitions = new Dictionary<BatteryStatus, BatteryStatus[]>
    {
        [BatteryStatus.Full] = new[] { BatteryStatus.Charging, BatteryStatus.Defective },
        [BatteryStatus.Charging] = new[] { BatteryStatus.Defective },
        [BatteryStatus.Taken] = new[] { BatteryStatus.Defective },
        [BatteryStatus.Booked] = Array.Empty<BatteryStatus>(),
        [BatteryStatus.Defective] = new[] { BatteryStatus.Charging }
    };

    private static readonly IReadOnlyDictionary<StaffShiftSlot, string[]> ShiftDutyMatrix = new Dictionary<StaffShiftSlot, string[]>
    {
        [StaffShiftSlot.Morning] = new[]
        {
            "Pre-open battery diagnostics & pairing health checks.",
            "Prepare reserved packs and confirm locker availability.",
            "Clear overnight support tickets before first riders arrive."
        },
        [StaffShiftSlot.Afternoon] = new[]
        {
            "Monitor peak-hour swaps and keep chargers balanced.",
            "Update swap log with SoH deltas for each transaction.",
            "Escalate any defective returns to maintenance queue."
        },
        [StaffShiftSlot.Night] = new[]
        {
            "Lock down kiosks and start slow charge cycle planning.",
            "Audit incomplete swaps and verify payment captures.",
            "Hand off unresolved tickets to the morning crew."
        }
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

    public async Task<IActionResult> OnPostProcessSupportAsync(int supportId, SupportStatus status, string? staffNote)
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

        var trimmedNote = string.IsNullOrWhiteSpace(staffNote) ? null : staffNote.Trim();
        await _supportService.UpdateSupportStatusAsync(supportId, status, null, trimmedNote);
        await LogActivityAsync("Support", $"Updated ticket #{supportId} -> {status}");

        TempData["SuccessMessage"] = "Support ticket updated.";
        SupportSuccessMessage = "Support ticket updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostHandleSwapActionAsync(int transactionId, SwapStatus status)
    {
        await LoadStationDataAsync();
        if (!HasStation)
        {
            TempData["ErrorMessage"] = "You are not assigned to any station.";
            return RedirectToPage();
        }

        var transaction = await _swapTransactionService.GetTransactionAsync(transactionId);
        if (transaction == null || transaction.StationId != Assignment!.StationId)
        {
            TempData["ErrorMessage"] = "Swap transaction not found for your station.";
            return RedirectToPage();
        }

        if (transaction.Status != SwapStatus.Pending)
        {
            TempData["ErrorMessage"] = "Only pending swaps can be actioned from this workspace.";
            return RedirectToPage();
        }

        if (status != SwapStatus.Completed && status != SwapStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "Unsupported swap status.";
            return RedirectToPage();
        }

        await _swapTransactionService.UpdateTransactionStatusAsync(transactionId, status);
        await LogActivityAsync("Swap", $"Swap #{transactionId} -> {status} (staff portal)");
        TempData["SuccessMessage"] = status == SwapStatus.Completed
            ? "Swap marked as completed."
            : "Swap cancelled.";

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
        ShiftInfo = StaffShiftViewModel.Create(Assignment, StationInfo);

        var stationSwaps = (await _swapTransactionService.GetTransactionsByStationAsync(Assignment.StationId))
            .OrderByDescending(tx => tx.SwapTime)
            .Take(12)
            .ToList();
        SwapWorkflow = SwapWorkflowViewModel.FromTransactions(stationSwaps, StationInfo?.Name ?? string.Empty);
        SupportSummary = SupportSummaryViewModel.FromTickets(SupportTickets);
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

    private static StaffShiftSlot ResolveShiftSlot(DateTime localTime)
    {
        var hour = localTime.Hour;
        if (hour >= 6 && hour < 14)
        {
            return StaffShiftSlot.Morning;
        }

        if (hour >= 14 && hour < 22)
        {
            return StaffShiftSlot.Afternoon;
        }

        return StaffShiftSlot.Night;
    }

    private static (DateTime Start, DateTime End) GetShiftBounds(DateTime localNow, StaffShiftSlot slot)
    {
        var date = localNow.Date;
        DateTime start;
        switch (slot)
        {
            case StaffShiftSlot.Morning:
                start = date.AddHours(6);
                break;
            case StaffShiftSlot.Afternoon:
                start = date.AddHours(14);
                break;
            default:
                if (localNow.Hour < 6)
                {
                    date = date.AddDays(-1);
                }
                start = date.AddHours(22);
                break;
        }

        var end = start.AddHours(8);
        return (start, end);
    }

    public class StaffShiftViewModel
    {
        public string ShiftName { get; init; } = string.Empty;
        public string Window { get; init; } = string.Empty;
        public string StationName { get; init; } = string.Empty;
        public DateTime ShiftStart { get; init; }
        public DateTime ShiftEnd { get; init; }
        public DateTime AssignedAtLocal { get; init; }
        public double ProgressPercent { get; init; }
        public TimeSpan TimeRemaining { get; init; }
        public IReadOnlyList<string> Checklist { get; init; } = Array.Empty<string>();

        public static StaffShiftViewModel? Create(StationStaff? assignment, ChangingStation? station)
        {
            if (assignment == null)
            {
                return null;
            }

            var localNow = DateTime.UtcNow.ToLocalTime();
            var slot = ResolveShiftSlot(localNow);
            var bounds = GetShiftBounds(localNow, slot);
            var duration = bounds.End - bounds.Start;
            var elapsed = localNow - bounds.Start;
            var progress = duration.TotalMinutes <= 0 ? 0 : elapsed.TotalMinutes / duration.TotalMinutes;
            progress = Math.Max(0, Math.Min(1, progress));

            var remaining = bounds.End - localNow;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            var duties = ShiftDutyMatrix.TryGetValue(slot, out var list) ? list : Array.Empty<string>();

            return new StaffShiftViewModel
            {
                ShiftName = slot switch
                {
                    StaffShiftSlot.Morning => "Morning shift",
                    StaffShiftSlot.Afternoon => "Afternoon shift",
                    StaffShiftSlot.Night => "Night shift",
                    _ => slot.ToString()
                },
                Window = $"{bounds.Start:HH:mm} - {bounds.End:HH:mm}",
                StationName = station?.Name ?? "Assigned station",
                ShiftStart = bounds.Start,
                ShiftEnd = bounds.End,
                AssignedAtLocal = assignment.AssignedAt.ToLocalTime(),
                ProgressPercent = progress * 100,
                TimeRemaining = remaining,
                Checklist = duties
            };
        }
    }

    public class SwapWorkflowViewModel
    {
        public static SwapWorkflowViewModel Empty { get; } = new()
        {
            Steps = Array.Empty<SwapWorkflowStep>(),
            StatusCounts = Enum.GetValues<SwapStatus>().ToDictionary(status => status, _ => 0),
            Timeline = Array.Empty<SwapWorkflowTimelineItem>()
        };

        public IReadOnlyList<SwapWorkflowStep> Steps { get; init; } = Array.Empty<SwapWorkflowStep>();
        public IReadOnlyDictionary<SwapStatus, int> StatusCounts { get; init; } = new Dictionary<SwapStatus, int>();
        public IReadOnlyList<SwapWorkflowTimelineItem> Timeline { get; init; } = Array.Empty<SwapWorkflowTimelineItem>();

        public static SwapWorkflowViewModel FromTransactions(IEnumerable<SwapTransaction>? transactions, string stationName)
        {
            var list = transactions?.ToList() ?? new List<SwapTransaction>();
            var counts = Enum.GetValues<SwapStatus>()
                .ToDictionary(status => status, status => list.Count(t => t.Status == status));

            counts.TryGetValue(SwapStatus.Pending, out var pending);
            counts.TryGetValue(SwapStatus.Completed, out var completed);

            var steps = new List<SwapWorkflowStep>
            {
                new SwapWorkflowStep(
                    "1. Check-in",
                    "Validate rider, reservation, and locker PIN.",
                    pending > 0,
                    pending == 0 && (completed > 0 || list.Count == 0)),
                new SwapWorkflowStep(
                    "2. Swap & QA",
                    "Pair the outgoing battery, log defects on returns, and confirm SoH.",
                    pending > 0,
                    completed > 0),
                new SwapWorkflowStep(
                    "3. Close-out",
                    "Collect payment or confirm auto-charge, then sync telemetry.",
                    completed > 0,
                    completed > 2)
            };

            var timeline = list
                .OrderByDescending(t => t.SwapTime)
                .Take(5)
                .Select(t => new SwapWorkflowTimelineItem(
                    t.TransactionId,
                    t.User?.FullName ?? $"User {t.UserId}",
                    t.Vehicle?.Vin ?? "N/A",
                    t.Status,
                    t.SwapTime.ToLocalTime(),
                    DateTime.UtcNow - t.SwapTime))
                .ToList();

            return new SwapWorkflowViewModel
            {
                Steps = steps,
                StatusCounts = counts,
                Timeline = timeline
            };
        }
    }

    public record SwapWorkflowStep(string Title, string Description, bool IsActive, bool IsCompleted);

    public record SwapWorkflowTimelineItem(
        int TransactionId,
        string RiderName,
        string Vehicle,
        SwapStatus Status,
        DateTime SwapTime,
        TimeSpan Age);

    public class SupportSummaryViewModel
    {
        public static SupportSummaryViewModel Empty { get; } = new();

        public int Total { get; init; }
        public int Open { get; init; }
        public int InProgress { get; init; }
        public int Closed { get; init; }
        public TimeSpan? OldestActiveAge { get; init; }

        public bool HasBacklogRisk => OldestActiveAge.HasValue && OldestActiveAge.Value > TimeSpan.FromHours(1);

        public static SupportSummaryViewModel FromTickets(IEnumerable<Support>? tickets)
        {
            var list = tickets?.ToList() ?? new List<Support>();
            var open = list.Count(t => t.Status == SupportStatus.Open);
            var inProgress = list.Count(t => t.Status == SupportStatus.InProgress);
            var closed = list.Count(t => t.Status == SupportStatus.Closed);

            var oldestActive = list
                .Where(t => t.Status != SupportStatus.Closed)
                .Select(t => (TimeSpan?)(DateTime.UtcNow - t.CreatedAt))
                .OrderByDescending(t => t)
                .FirstOrDefault();

            return new SupportSummaryViewModel
            {
                Total = list.Count,
                Open = open,
                InProgress = inProgress,
                Closed = closed,
                OldestActiveAge = oldestActive
            };
        }
    }

    private enum StaffShiftSlot
    {
        Morning,
        Afternoon,
        Night
    }
}

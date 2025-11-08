using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BSMS.WebApp.Pages;

namespace BSMS.WebApp.Pages.Admin.Operations;

public class IndexModel : BasePageModel
{
    private readonly IChangingStationService _stationService;
    private readonly IBatteryService _batteryService;
    private readonly IBatteryTransferService _transferService;
    private readonly IStationStaffService _stationStaffService;
    private readonly ISupportService _supportService;
    private readonly ISwapTransactionService _swapTransactionService;
    private readonly IConfigService _configService;
    private readonly IUserService _userService;

    public IndexModel(
        IChangingStationService stationService,
        IBatteryService batteryService,
        IBatteryTransferService transferService,
        IStationStaffService stationStaffService,
        ISupportService supportService,
        ISwapTransactionService swapTransactionService,
        IConfigService configService,
        IUserService userService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _stationService = stationService;
        _batteryService = batteryService;
        _transferService = transferService;
        _stationStaffService = stationStaffService;
        _supportService = supportService;
        _swapTransactionService = swapTransactionService;
        _configService = configService;
        _userService = userService;
    }

    public OperationsOverview Overview { get; set; } = new();
    public IList<ChangingStation> Stations { get; set; } = new List<ChangingStation>();
    public IList<Battery> Batteries { get; set; } = new List<Battery>();
    public IList<BatteryTransfer> Transfers { get; set; } = new List<BatteryTransfer>();
    public IList<StationStaff> Assignments { get; set; } = new List<StationStaff>();
    public IList<Support> Supports { get; set; } = new List<Support>();
    public IList<SwapTransaction> SwapTransactions { get; set; } = new List<SwapTransaction>();
    public IList<Config> Configs { get; set; } = new List<Config>();

    public List<SelectListItem> StationOptions { get; set; } = new();
    public List<SelectListItem> StaffUserOptions { get; set; } = new();
    public List<SelectListItem> SupportUserOptions { get; set; } = new();
    public List<SelectListItem> BatteryOptions { get; set; } = new();

    public IEnumerable<SelectListItem> StationStatusOptions =>
        Enum.GetValues<StationStatus>()
            .Cast<StationStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public IEnumerable<SelectListItem> BatteryStatusOptions =>
        Enum.GetValues<BatteryStatus>()
            .Cast<BatteryStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public IEnumerable<SelectListItem> TransferStatusOptions =>
        Enum.GetValues<TransferStatus>()
            .Cast<TransferStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public IEnumerable<SelectListItem> SupportStatusOptions =>
        Enum.GetValues<SupportStatus>()
            .Cast<SupportStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public IEnumerable<SelectListItem> SupportTypeOptions =>
        Enum.GetValues<SupportType>()
            .Cast<SupportType>()
            .Select(type => new SelectListItem(type.ToString(), type.ToString()));

    public IEnumerable<SelectListItem> SwapStatusOptions =>
        Enum.GetValues<SwapStatus>()
            .Cast<SwapStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    [BindProperty]
    public StationInput StationForm { get; set; } = new();

    [BindProperty]
    public BatteryInput BatteryForm { get; set; } = new();

    [BindProperty]
    public TransferInput TransferForm { get; set; } = new();

    [BindProperty]
    public StationStaffInput StationStaffForm { get; set; } = new();

    [BindProperty]
    public SupportInput SupportForm { get; set; } = new();

    [BindProperty]
    public ConfigInput ConfigForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadReferenceDataAsync();
    }

    public async Task<IActionResult> OnPostSaveStationAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceDataAsync();
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

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteStationAsync(int stationId)
    {
        try
        {
            await _stationService.DeleteStationAsync(stationId);
            TempData["SuccessMessage"] = "Đã xoá trạm.";
            await LogActivityAsync("Station", $"Deleted station {stationId}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveBatteryAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        try
        {
            if (BatteryForm.BatteryId == 0)
            {
                await _batteryService.CreateBatteryAsync(new Battery
                {
                    Model = BatteryForm.Model,
                    Capacity = BatteryForm.Capacity,
                    Soh = BatteryForm.Soh,
                    Status = BatteryForm.Status,
                    StationId = BatteryForm.StationId,
                    LastMaintenance = BatteryForm.LastMaintenance
                });
                TempData["SuccessMessage"] = "Đã thêm pin mới.";
                await LogActivityAsync("Battery", $"Created battery {BatteryForm.Model}");
            }
            else
            {
                await _batteryService.UpdateBatteryAsync(new Battery
                {
                    BatteryId = BatteryForm.BatteryId,
                    Model = BatteryForm.Model,
                    Capacity = BatteryForm.Capacity,
                    Soh = BatteryForm.Soh,
                    Status = BatteryForm.Status,
                    StationId = BatteryForm.StationId,
                    LastMaintenance = BatteryForm.LastMaintenance
                });
                TempData["SuccessMessage"] = "Đã cập nhật pin.";
                await LogActivityAsync("Battery", $"Updated battery #{BatteryForm.BatteryId}");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteBatteryAsync(int batteryId)
    {
        try
        {
            await _batteryService.DeleteBatteryAsync(batteryId);
            TempData["SuccessMessage"] = "Đã xoá pin khỏi hệ thống.";
            await LogActivityAsync("Battery", $"Deleted battery {batteryId}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveTransferAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        if (TransferForm.FromStationId == TransferForm.ToStationId)
        {
            ModelState.AddModelError(string.Empty, "Điểm đi và đến không được trùng nhau.");
            await LoadReferenceDataAsync();
            return Page();
        }

        try
        {
            if (TransferForm.TransferId == 0)
            {
                await _transferService.CreateTransferAsync(new BatteryTransfer
                {
                    BatteryId = TransferForm.BatteryId,
                    FromStationId = TransferForm.FromStationId,
                    ToStationId = TransferForm.ToStationId,
                    TransferTime = TransferForm.TransferTime ?? DateTime.UtcNow,
                    Status = TransferForm.Status
                });

                TempData["SuccessMessage"] = "Đã lên lịch điều phối pin.";
                await LogActivityAsync("Transfer", $"Scheduled transfer for battery {TransferForm.BatteryId}");
            }
            else
            {
                await _transferService.UpdateTransferAsync(new BatteryTransfer
                {
                    TransferId = TransferForm.TransferId,
                    BatteryId = TransferForm.BatteryId,
                    FromStationId = TransferForm.FromStationId,
                    ToStationId = TransferForm.ToStationId,
                    TransferTime = TransferForm.TransferTime ?? DateTime.UtcNow,
                    Status = TransferForm.Status
                });

                TempData["SuccessMessage"] = "Đã cập nhật luồng điều phối.";
                await LogActivityAsync("Transfer", $"Updated transfer #{TransferForm.TransferId}");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateTransferStatusAsync(int transferId, TransferStatus status)
    {
        try
        {
            await _transferService.UpdateTransferStatusAsync(transferId, status);
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái điều phối.";
            await LogActivityAsync("Transfer", $"Changed transfer #{transferId} to {status}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteTransferAsync(int transferId)
    {
        try
        {
            await _transferService.DeleteTransferAsync(transferId);
            TempData["SuccessMessage"] = "Đã huỷ điều phối.";
            await LogActivityAsync("Transfer", $"Deleted transfer {transferId}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveStationStaffAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        try
        {
            if (StationStaffForm.StaffId == 0)
            {
                await _stationStaffService.AssignStaffAsync(new StationStaff
                {
                    UserId = StationStaffForm.UserId,
                    StationId = StationStaffForm.StationId,
                    AssignedAt = DateTime.UtcNow
                });

                TempData["SuccessMessage"] = "Đã phân công nhân sự.";
                await LogActivityAsync("Staff", $"Assigned user {StationStaffForm.UserId} to station {StationStaffForm.StationId}");
            }
            else
            {
                await _stationStaffService.UpdateAssignmentAsync(new StationStaff
                {
                    StaffId = StationStaffForm.StaffId,
                    UserId = StationStaffForm.UserId,
                    StationId = StationStaffForm.StationId,
                    AssignedAt = StationStaffForm.AssignedAt ?? DateTime.UtcNow
                });

                TempData["SuccessMessage"] = "Đã cập nhật phân công nhân sự.";
                await LogActivityAsync("Staff", $"Updated assignment #{StationStaffForm.StaffId}");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteStationStaffAsync(int staffId)
    {
        try
        {
            await _stationStaffService.RemoveAssignmentAsync(staffId);
            TempData["SuccessMessage"] = "Đã gỡ nhân sự khỏi trạm.";
            await LogActivityAsync("Staff", $"Removed assignment {staffId}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveSupportAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        try
        {
            if (SupportForm.SupportId == 0)
            {
                await _supportService.CreateSupportAsync(new Support
                {
                    UserId = SupportForm.UserId,
                    StationId = SupportForm.StationId,
                    Type = SupportForm.Type,
                    Description = SupportForm.Description,
                    Status = SupportForm.Status,
                    Rating = SupportForm.Rating
                });

                TempData["SuccessMessage"] = "Đã tạo ticket hỗ trợ.";
                await LogActivityAsync("Support", $"Created support ticket for user {SupportForm.UserId}");
            }
            else
            {
                await _supportService.UpdateSupportAsync(new Support
                {
                    SupportId = SupportForm.SupportId,
                    UserId = SupportForm.UserId,
                    StationId = SupportForm.StationId,
                    Type = SupportForm.Type,
                    Description = SupportForm.Description,
                    Status = SupportForm.Status,
                    Rating = SupportForm.Rating
                });

                TempData["SuccessMessage"] = "Đã cập nhật ticket hỗ trợ.";
                await LogActivityAsync("Support", $"Updated ticket #{SupportForm.SupportId}");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateSupportStatusAsync(int supportId, SupportStatus status, int? rating)
    {
        try
        {
            await _supportService.UpdateSupportStatusAsync(supportId, status, rating);
            TempData["SuccessMessage"] = "Đã cập nhật xử lý khiếu nại.";
            await LogActivityAsync("Support", $"Support #{supportId} -> {status}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteSupportAsync(int supportId)
    {
        try
        {
            await _supportService.DeleteSupportAsync(supportId);
            TempData["SuccessMessage"] = "Đã xoá ticket hỗ trợ.";
            await LogActivityAsync("Support", $"Deleted ticket {supportId}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateSwapStatusAsync(int transactionId, SwapStatus status)
    {
        try
        {
            await _swapTransactionService.UpdateTransactionStatusAsync(transactionId, status);
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái giao dịch đổi pin.";
            await LogActivityAsync("Swap", $"Swap #{transactionId} -> {status}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteSwapAsync(int transactionId)
    {
        try
        {
            await _swapTransactionService.DeleteTransactionAsync(transactionId);
            TempData["SuccessMessage"] = "Đã xoá giao dịch.";
            await LogActivityAsync("Swap", $"Deleted swap {transactionId}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveConfigAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        try
        {
            await _configService.SaveAsync(new Config
            {
                ConfigId = ConfigForm.ConfigId,
                Name = ConfigForm.Name,
                Value = ConfigForm.Value,
                Description = ConfigForm.Description ?? string.Empty
            });

            TempData["SuccessMessage"] = "Đã lưu cấu hình hệ thống.";
            await LogActivityAsync("Config", $"Saved config {ConfigForm.Name}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteConfigAsync(int configId)
    {
        try
        {
            await _configService.DeleteAsync(configId);
            TempData["SuccessMessage"] = "Đã xoá cấu hình.";
            await LogActivityAsync("Config", $"Deleted config {configId}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    private async Task LoadReferenceDataAsync()
    {
        var stationList = (await _stationService.GetStationsWithDetailsAsync()).ToList();
        var batteryList = (await _batteryService.GetBatteriesAsync()).ToList();
        var transfersList = (await _transferService.GetRecentTransfersAsync(25)).ToList();
        var staffAssignments = (await _stationStaffService.GetAssignmentsAsync()).ToList();
        var supportList = (await _supportService.GetOpenSupportsAsync()).ToList();
        var swapList = (await _swapTransactionService.GetRecentTransactionsAsync(20)).ToList();
        var configList = (await _configService.GetAllAsync()).ToList();
        var staffUsers = (await _userService.GetUsersByRoleAsync(UserRole.StationStaff)).ToList();
        var driverUsers = (await _userService.GetUsersByRoleAsync(UserRole.Driver)).ToList();
        var transfersInProgress = (await _transferService.GetTransfersInProgressAsync()).ToList();
        var monthlyRevenue = await _swapTransactionService.GetCurrentMonthRevenueAsync();
        var dailyTransactions = await _swapTransactionService.GetDailyTransactionCountAsync(DateTime.UtcNow);
        var batterySummary = await _batteryService.GetStatusSummaryAsync();

        Stations = stationList.OrderBy(s => s.Name).ToList();
        Batteries = batteryList.OrderByDescending(b => b.UpdatedAt).Take(50).ToList();
        Transfers = transfersList;
        Assignments = staffAssignments;
        Supports = supportList;
        SwapTransactions = swapList;
        Configs = configList;

        StationOptions = stationList
            .Select(s => new SelectListItem
            {
                Text = $"{s.Name} ({s.Status})",
                Value = s.StationId.ToString()
            })
            .ToList();

        BatteryOptions = batteryList
            .Select(b => new SelectListItem
            {
                Text = $"#{b.BatteryId} - {b.Model}",
                Value = b.BatteryId.ToString()
            })
            .ToList();

        StaffUserOptions = staffUsers
            .Select(u => new SelectListItem
            {
                Text = u.FullName ?? u.Username ?? $"User {u.UserId}",
                Value = u.UserId.ToString()
            })
            .ToList();

        SupportUserOptions = driverUsers
            .Concat(staffUsers)
            .GroupBy(u => u.UserId)
            .Select(g => g.First())
            .Select(u => new SelectListItem
            {
                Text = u.FullName ?? u.Username ?? $"User {u.UserId}",
                Value = u.UserId.ToString()
            })
            .OrderBy(s => s.Text)
            .ToList();

        Overview = new OperationsOverview
        {
            StationCount = Stations.Count,
            ActiveStationCount = Stations.Count(s => s.Status == StationStatus.Active),
            BatteryTotal = batteryList.Count,
            BatteryStatusSummary = batterySummary,
            TransfersInProgress = transfersInProgress.Count,
            OpenSupports = Supports.Count,
            MonthlyRevenue = monthlyRevenue,
            DailyTransactions = dailyTransactions,
            GeneratedAt = DateTime.UtcNow
        };

        StationForm ??= new StationInput { Status = StationStatus.Active };
        BatteryForm ??= new BatteryInput { Status = BatteryStatus.Full };
        TransferForm ??= new TransferInput { Status = TransferStatus.InProgress };
        StationStaffForm ??= new StationStaffInput();
        SupportForm ??= new SupportInput { Status = SupportStatus.Open };
        ConfigForm ??= new ConfigInput();
    }

    public class OperationsOverview
    {
        public int StationCount { get; set; }
        public int ActiveStationCount { get; set; }
        public int BatteryTotal { get; set; }
        public Dictionary<BatteryStatus, int> BatteryStatusSummary { get; set; } = new();
        public int TransfersInProgress { get; set; }
        public int OpenSupports { get; set; }
        public int DailyTransactions { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class StationInput
    {
        public int StationId { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(1, 1000)]
        public int Capacity { get; set; }

        [Required]
        public StationStatus Status { get; set; } = StationStatus.Active;
    }

    public class BatteryInput
    {
        public int BatteryId { get; set; }

        [Required, StringLength(100)]
        public string Model { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int Capacity { get; set; }

        [Range(0, 100)]
        public decimal Soh { get; set; }

        [Required]
        public BatteryStatus Status { get; set; } = BatteryStatus.Full;

        [Required]
        public int StationId { get; set; }

        public DateTime? LastMaintenance { get; set; }
    }

    public class TransferInput
    {
        public int TransferId { get; set; }

        [Required]
        public int BatteryId { get; set; }

        [Required]
        public int FromStationId { get; set; }

        [Required]
        public int ToStationId { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.InProgress;

        public DateTime? TransferTime { get; set; }
    }

    public class StationStaffInput
    {
        public int StaffId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int StationId { get; set; }

        public DateTime? AssignedAt { get; set; }
    }

    public class SupportInput
    {
        public int SupportId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int StationId { get; set; }

        [Required]
        public SupportType Type { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public SupportStatus Status { get; set; } = SupportStatus.Open;

        [Range(1, 5)]
        public int? Rating { get; set; }
    }

    public class ConfigInput
    {
        public int ConfigId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}

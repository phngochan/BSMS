using BSMS.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using BSMS.WebApp.Pages;
using Microsoft.EntityFrameworkCore;

namespace BSMS.WebApp.Pages.Admin.Operations;

[Authorize(Roles = "Admin")]
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
    private readonly IPasswordHasher _passwordHasher;
    private readonly IReservationService _reservationService;
    private static readonly TimeSpan DriverQueueWindow = TimeSpan.FromHours(6);
    private const int UpcomingReservationDisplayLimit = 12;

    public IndexModel(
        IChangingStationService stationService,
        IBatteryService batteryService,
        IBatteryTransferService transferService,
        IStationStaffService stationStaffService,
        ISupportService supportService,
        ISwapTransactionService swapTransactionService,
        IConfigService configService,
        IUserService userService,
        IReservationService reservationService,
        IPasswordHasher passwordHasher,
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
        _passwordHasher = passwordHasher;
        _reservationService = reservationService;
    }

    public OperationsOverview Overview { get; set; } = new();
    public IList<ChangingStation> Stations { get; set; } = new List<ChangingStation>();
    public IList<Battery> Batteries { get; set; } = new List<Battery>();
    public IList<Battery> TransferEligibleBatteries { get; set; } = new List<Battery>();
    public IList<BatteryTransfer> Transfers { get; set; } = new List<BatteryTransfer>();
    public IList<StationStaff> Assignments { get; set; } = new List<StationStaff>();
    public IList<Support> Supports { get; set; } = new List<Support>();
    public IList<SwapTransaction> SwapTransactions { get; set; } = new List<SwapTransaction>();
    public IList<Config> Configs { get; set; } = new List<Config>();
    public HashSet<int> IdleStations { get; private set; } = new();
    public HashSet<int> StationsWithoutStaff { get; private set; } = new(); // ✅ THÊM

    public IList<Reservation> UpcomingReservations { get; private set; } = new List<Reservation>();
    public Dictionary<int, StationDriverSignal> StationDriverSignals { get; private set; } = new();

    public List<SelectListItem> StationOptions { get; set; } = new();
    public List<SelectListItem> StaffUserOptions { get; set; } = new();
    public List<SelectListItem> SupportUserOptions { get; set; } = new();

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

    [BindProperty]
    public StaffUserCreateInput StaffUserCreateForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadReferenceDataAsync();
    }

    public async Task<IActionResult> OnPostSaveStationAsync()
    {
        if (!ValidateForm(StationForm, nameof(StationForm)))
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        try
        {
            var validationMessage = await ValidateStationStatusRulesAsync();
            if (!string.IsNullOrEmpty(validationMessage))
            {
                TempData["ErrorMessage"] = validationMessage;
                await LoadReferenceDataAsync();
                return Page();
            }

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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveBatteryAsync()
    {
        if (!ValidateForm(BatteryForm, nameof(BatteryForm)))
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        if (BatteryForm.Status == BatteryStatus.Defective && string.IsNullOrWhiteSpace(BatteryForm.DefectNote))
        {
            ModelState.AddModelError($"{nameof(BatteryForm)}.{nameof(BatteryForm.DefectNote)}", "Please provide the reason when marking a battery as defective.");
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
                    LastMaintenance = BatteryForm.LastMaintenance,
                    DefectNote = BatteryForm.DefectNote
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
                    LastMaintenance = BatteryForm.LastMaintenance,
                    DefectNote = BatteryForm.DefectNote
                });
                TempData["SuccessMessage"] = "Đã cập nhật pin.";
                await LogActivityAsync("Battery", $"Updated battery #{BatteryForm.BatteryId}");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveTransferAsync()
    {
        if (!ValidateForm(TransferForm, nameof(TransferForm)))
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

        var nowUtc = DateTime.UtcNow;
        var transferTimeUtc = nowUtc;

        if (TransferForm.TransferTime.HasValue)
        {
            transferTimeUtc = NormalizeToUtc(TransferForm.TransferTime.Value);
            if (transferTimeUtc <= nowUtc)
            {
                ModelState.AddModelError($"{nameof(TransferForm)}.{nameof(TransferForm.TransferTime)}", "Thời gian điều phối phải lớn hơn thời điểm hiện tại.");
                await LoadReferenceDataAsync();
                return Page();
            }
        }

        try
        {
            // ✅ THÊM VALIDATION CHO TRƯỜNG HỢP TẠO MỚI
            if (TransferForm.TransferId == 0)
            {
                var battery = await _batteryService.GetBatteryAsync(TransferForm.BatteryId);
                
                if (battery == null)
                {
                    ModelState.AddModelError(string.Empty, "Không tìm thấy pin.");
                    await LoadReferenceDataAsync();
                    return Page();
                }

                // Kiểm tra pin phải ở trạng thái Full
                if (battery.Status != BatteryStatus.Full)
                {
                    ModelState.AddModelError(string.Empty, $"Pin chỉ có thể điều phối khi trạng thái = Full. Hiện tại: {battery.Status}");
                    await LoadReferenceDataAsync();
                    return Page();
                }

                // Kiểm tra pin phải ở đúng trạm xuất phát
                if (battery.StationId != TransferForm.FromStationId)
                {
                    ModelState.AddModelError(string.Empty, $"Pin hiện đang ở trạm #{battery.StationId}, không phải trạm xuất phát đã chọn.");
                    await LoadReferenceDataAsync();
                    return Page();
                }
            }

            if (TransferForm.TransferId == 0)
            {
                await _transferService.CreateTransferAsync(new BatteryTransfer
                {
                    BatteryId = TransferForm.BatteryId,
                    FromStationId = TransferForm.FromStationId,
                    ToStationId = TransferForm.ToStationId,
                    TransferTime = transferTimeUtc,
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
                    TransferTime = transferTimeUtc,
                    Status = TransferForm.Status
                });

                TempData["SuccessMessage"] = "Đã cập nhật luồng điều phối.";
                await LogActivityAsync("Transfer", $"Updated transfer #{TransferForm.TransferId}");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateTransferStatusAsync(int transferId, TransferStatus status)
    {
        try
        {
            // ✅ THÊM LOGIC DI CHUYỂN PIN KHI HOÀN TẤT
            if (status == TransferStatus.Completed)
            {
                var transfer = await _transferService.GetTransferAsync(transferId);
                if (transfer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lệnh điều phối.";
                    return RedirectToPage();
                }

                var battery = await _batteryService.GetBatteryAsync(transfer.BatteryId);
                if (battery != null)
                {
                    // Di chuyển pin sang trạm đích
                    battery.StationId = transfer.ToStationId;
                    await _batteryService.UpdateBatteryAsync(battery);
                }
            }

            await _transferService.UpdateTransferStatusAsync(transferId, status);
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái điều phối.";
            await LogActivityAsync("Transfer", $"Changed transfer #{transferId} to {status}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveStationStaffAsync()
    {
        if (!ValidateForm(StationStaffForm, nameof(StationStaffForm)))
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        var nowUtc = DateTime.UtcNow;
        DateTime assignedAtUtc = StationStaffForm.AssignedAt.HasValue
            ? NormalizeToUtc(StationStaffForm.AssignedAt.Value)
            : nowUtc;

        if (StationStaffForm.StaffId == 0 && assignedAtUtc <= nowUtc)
        {
            ModelState.AddModelError($"{nameof(StationStaffForm)}.{nameof(StationStaffForm.AssignedAt)}", "Thời điểm phân bổ phải nằm trong tương lai.");
            await LoadReferenceDataAsync();
            return Page();
        }

        var station = await _stationService.GetStationAsync(StationStaffForm.StationId);
        if (station == null || station.Status != StationStatus.Active)
        {
            ModelState.AddModelError($"{nameof(StationStaffForm)}.{nameof(StationStaffForm.StationId)}", "Chỉ có thể phân bổ tới trạm đang hoạt động.");
            await LoadReferenceDataAsync();
            return Page();
        }

        var existingAssignment = await _stationStaffService.GetAssignmentForUserAsync(StationStaffForm.UserId);
        if (existingAssignment != null && (StationStaffForm.StaffId == 0 || existingAssignment.StaffId != StationStaffForm.StaffId))
        {
            var conflictMessage = existingAssignment.StationId == StationStaffForm.StationId
                ? "Nhân viên này đã được phân bổ cho trạm này."
                : $"Nhân viên này đang được phân bổ tại trạm #{existingAssignment.StationId}.";
            ModelState.AddModelError($"{nameof(StationStaffForm)}.{nameof(StationStaffForm.UserId)}", conflictMessage);
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
                    AssignedAt = assignedAtUtc
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
                    AssignedAt = assignedAtUtc
                });

                TempData["SuccessMessage"] = "Đã cập nhật phân công nhân sự.";
                await LogActivityAsync("Staff", $"Updated assignment #{StationStaffForm.StaffId}");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCreateStaffUserAsync()
    {
        if (!ValidateForm(StaffUserCreateForm, nameof(StaffUserCreateForm)))
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        if (await _userService.IsUsernameExistsAsync(StaffUserCreateForm.Username.Trim()))
        {
            ModelState.AddModelError($"{nameof(StaffUserCreateForm)}.{nameof(StaffUserCreateForm.Username)}", "Tên đăng nhập đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(StaffUserCreateForm.Email) &&
            await _userService.IsEmailExistsAsync(StaffUserCreateForm.Email.Trim()))
        {
            ModelState.AddModelError($"{nameof(StaffUserCreateForm)}.{nameof(StaffUserCreateForm.Email)}", "Email đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            await LoadReferenceDataAsync();
            return Page();
        }

        var newUser = new User
        {
            Username = StaffUserCreateForm.Username.Trim(),
            FullName = StaffUserCreateForm.FullName.Trim(),
            Email = StaffUserCreateForm.Email?.Trim() ?? string.Empty,
            Phone = StaffUserCreateForm.Phone?.Trim() ?? string.Empty,
            Role = UserRole.StationStaff,
            PasswordHash = _passwordHasher.HashPassword(StaffUserCreateForm.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userService.CreateUserAsync(newUser);
        TempData["SuccessMessage"] = "Đã tạo nhân sự mới.";
        await LogActivityAsync("Staff", $"Created staff user {newUser.Username}");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteStationStaffAsync(int staffId)
    {
        try
        {
            // ✅ THÊM VALIDATION
            var assignment = await _stationStaffService.GetAssignmentAsync(staffId);
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phân công.";
                return RedirectToPage();
            }

            var station = await _stationService.GetStationAsync(assignment.StationId);
            if (station == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy trạm.";
                return RedirectToPage();
            }

            // Kiểm tra nếu trạm Active và đây là nhân viên cuối cùng
            if (station.Status == StationStatus.Active)
            {
                var staffCount = (await _stationStaffService.GetAssignmentsByStationAsync(assignment.StationId)).Count();
                if (staffCount <= 1)
                {
                    TempData["ErrorMessage"] = "Không thể gỡ nhân viên cuối cùng khỏi trạm đang Active. Vui lòng chuyển trạm sang Maintenance hoặc Inactive trước.";
                    await LoadReferenceDataAsync();
                    return Page();
                }
            }

            await _stationStaffService.RemoveAssignmentAsync(staffId);
            TempData["SuccessMessage"] = "Đã gỡ nhân sự khỏi trạm.";
            await LogActivityAsync("Staff", $"Removed assignment {staffId}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveSupportAsync()
    {
        if (!ValidateForm(SupportForm, nameof(SupportForm)))
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
                    StaffNote = SupportForm.StaffNote
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
                    StaffNote = SupportForm.StaffNote
                });

                TempData["SuccessMessage"] = "Đã cập nhật ticket hỗ trợ.";
                await LogActivityAsync("Support", $"Updated ticket #{SupportForm.SupportId}");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateSupportStatusAsync(int supportId, SupportStatus status)
    {
        try
        {
            await _supportService.UpdateSupportStatusAsync(supportId, status);
            TempData["SuccessMessage"] = "Đã cập nhật xử lý khiếu nại.";
            await LogActivityAsync("Support", $"Support #{supportId} -> {status}");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
            await LoadReferenceDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveConfigAsync()
    {
        if (!ValidateForm(ConfigForm, nameof(ConfigForm)))
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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
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
            TempData["ErrorMessage"] = GetFriendlyErrorMessage(ex);
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
        var supportList = (await _supportService.GetAllSupportsAsync()).ToList();
        var swapList = (await _swapTransactionService.GetRecentTransactionsAsync(20)).ToList();
        var configList = (await _configService.GetAllAsync()).ToList();
        var staffUsers = (await _userService.GetUsersByRoleAsync(UserRole.StationStaff)).ToList();
        var driverUsers = (await _userService.GetUsersByRoleAsync(UserRole.Driver)).ToList();
        var transfersInProgress = (await _transferService.GetTransfersInProgressAsync()).ToList();
        var monthlyRevenue = await _swapTransactionService.GetCurrentMonthRevenueAsync();
        var dailyTransactions = await _swapTransactionService.GetDailyTransactionCountAsync(DateTime.UtcNow);
        var batterySummary = await _batteryService.GetStatusSummaryAsync();
        var latestSwapTimes = await _swapTransactionService.GetLatestCompletedSwapTimesAsync();
        var openSupportCount = await _supportService.CountByStatusAsync(SupportStatus.Open);
        var stationLookup = stationList.ToDictionary(s => s.StationId);
        var batteryStatusByStation = batteryList
            .GroupBy(b => b.StationId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(b => b.Status)
                      .ToDictionary(bg => bg.Key, bg => bg.Count()));
        var now = DateTime.UtcNow;
        var queueWindowEnd = now.Add(DriverQueueWindow);
        var upcomingReservationWindow = (await _reservationService
            .GetUpcomingReservationsAsync(now, queueWindowEnd))
            .OrderBy(r => r.ScheduledTime)
            .ToList();
        var driverQueueCount = upcomingReservationWindow.Count;
        UpcomingReservations = upcomingReservationWindow
            .Take(UpcomingReservationDisplayLimit)
            .ToList();
        StationDriverSignals = upcomingReservationWindow
            .GroupBy(r => r.StationId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    batteryStatusByStation.TryGetValue(g.Key, out var statusCounts);
                    statusCounts ??= new Dictionary<BatteryStatus, int>();
                    return new StationDriverSignal
                    {
                        StationId = g.Key,
                        StationName = stationLookup.TryGetValue(g.Key, out var station)
                            ? station.Name
                            : $"Station {g.Key}",
                        PendingReservations = g.Count(),
                        FullBatteries = statusCounts.TryGetValue(BatteryStatus.Full, out var full) ? full : 0,
                        BookedBatteries = statusCounts.TryGetValue(BatteryStatus.Booked, out var booked) ? booked : 0,
                        DriverNames = g
                            .Select(r => r.User?.FullName ?? r.User?.Username ?? $"User {r.UserId}")
                            .Where(name => !string.IsNullOrWhiteSpace(name))
                            .Distinct()
                            .Take(3)
                            .ToList(),
                        NextReservationTime = g.Min(r => r.ScheduledTime)
                    };
                });

        Stations = stationList.OrderBy(s => s.Name).ToList();
        Batteries = batteryList.OrderByDescending(b => b.UpdatedAt).Take(50).ToList();
        TransferEligibleBatteries = batteryList
            .Where(b => b.Status == BatteryStatus.Full)
            .OrderBy(b => b.StationId)
            .ThenBy(b => b.BatteryId)
            .ToList();
        Transfers = transfersList;
        Assignments = staffAssignments;
        Supports = supportList;
        SwapTransactions = swapList;
        Configs = configList;
        
        var idleThreshold = TimeSpan.FromHours(12);
        IdleStations = stationList
            .Where(s =>
            {
                if (!latestSwapTimes.TryGetValue(s.StationId, out var lastSwap))
                {
                    return true;
                }
                return (now - lastSwap) > idleThreshold;
            })
            .Select(s => s.StationId)
            .ToHashSet();

        // ✅ THÊM: Tính trạm Active không có nhân viên
        var stationStaffCounts = staffAssignments
            .GroupBy(s => s.StationId)
            .ToDictionary(g => g.Key, g => g.Count());

        StationsWithoutStaff = stationList
            .Where(s => s.Status == StationStatus.Active && 
                        !stationStaffCounts.ContainsKey(s.StationId))
            .Select(s => s.StationId)
            .ToHashSet();

        StationOptions = stationList
            .Select(s => new SelectListItem
            {
                Text = $"{s.Name} ({s.Status})",
                Value = s.StationId.ToString()
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
            OpenSupports = openSupportCount,
            MonthlyRevenue = monthlyRevenue,
            DailyTransactions = dailyTransactions,
            DriverQueueCount = driverQueueCount,
            GeneratedAt = DateTime.UtcNow
        };

        StationForm ??= new StationInput { Status = StationStatus.Active };
        BatteryForm ??= new BatteryInput { Status = BatteryStatus.Full };
        TransferForm ??= new TransferInput { Status = TransferStatus.InProgress };
        StationStaffForm ??= new StationStaffInput();
        SupportForm ??= new SupportInput { Status = SupportStatus.Open };
        ConfigForm ??= new ConfigInput();
        StaffUserCreateForm ??= new StaffUserCreateInput();
    }

    private static DateTime NormalizeToUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => dateTime.ToUniversalTime()
        };
    }

    private bool ValidateForm<TModel>(TModel formModel, string prefix)
    {
        ModelState.Clear();
        var isValid = TryValidateModel(formModel, prefix);

        if (!isValid)
        {
            var allowedPrefix = $"{prefix}.";
            var keysToRemove = ModelState.Keys
                .Where(k => !k.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                    && !k.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
        }

        return isValid;
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
        public int DriverQueueCount { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class StationDriverSignal
    {
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public int PendingReservations { get; set; }
        public int FullBatteries { get; set; }
        public int BookedBatteries { get; set; }
        public List<string> DriverNames { get; set; } = new();
        public DateTime? NextReservationTime { get; set; }
        public bool NeedsAttention => PendingReservations > 0 && FullBatteries == 0;
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

        [Required(ErrorMessage = "Model là bắt buộc")]
        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Capacity là bắt buộc")]
        [Range(1, 10000, ErrorMessage = "Capacity phải từ 1 đến 10000 Wh")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "SoH là bắt buộc")]
        [Range(0, 100, ErrorMessage = "SoH phải từ 0 đến 100%")]
        public decimal Soh { get; set; } = 100;

        [Required]
        public BatteryStatus Status { get; set; } = BatteryStatus.Full;

        [Required(ErrorMessage = "Trạm là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn trạm")]
        public int StationId { get; set; }

        public DateTime? LastMaintenance { get; set; }

        [StringLength(500)]
        public string? DefectNote { get; set; }
    }

    public class TransferInput
    {
        public int TransferId { get; set; }

        [Required(ErrorMessage = "Pin là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn pin")]
        public int BatteryId { get; set; }

        [Required(ErrorMessage = "Trạm đi là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn trạm đi")]
        public int FromStationId { get; set; }

        [Required(ErrorMessage = "Trạm đến là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn trạm đến")]
        public int ToStationId { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.InProgress;

        public DateTime? TransferTime { get; set; }
    }

    public class StationStaffInput
    {
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Nhân sự là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhân sự")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Trạm là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn trạm")]
        public int StationId { get; set; }

        public DateTime? AssignedAt { get; set; }
    }

    public class SupportInput
    {
        public int SupportId { get; set; }

        [Required(ErrorMessage = "Người gửi là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn người gửi")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Trạm là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn trạm")]
        public int StationId { get; set; }

        [Required]
        public SupportType Type { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public SupportStatus Status { get; set; } = SupportStatus.Open;

        [StringLength(1000)]
        public string? StaffNote { get; set; }

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

    public class StaffUserCreateInput
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 4)]
        public string Username { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}


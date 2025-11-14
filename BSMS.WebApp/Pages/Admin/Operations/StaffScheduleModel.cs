using BSMS.BLL.Services;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using BSMS.WebApp.Hubs;
using System.ComponentModel.DataAnnotations;

namespace BSMS.WebApp.Pages.Admin.Operations;

[Authorize(Roles = "Admin")]
public class StaffScheduleModel : PageModel
{
    private readonly IStationStaffService _staffService;
    private readonly IUserService _userService;
    private readonly IChangingStationService _stationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<StaffScheduleModel> _logger;

    public StaffScheduleModel(
        IStationStaffService staffService,
        IUserService userService,
        IChangingStationService stationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<StaffScheduleModel> logger)
    {
        _staffService = staffService;
        _userService = userService;
        _stationService = stationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [BindProperty]
    public StaffAssignmentInput Input { get; set; } = new();

    public IEnumerable<User> AvailableStaff { get; set; } = new List<User>();
    public IEnumerable<ChangingStation> Stations { get; set; } = new List<ChangingStation>();
    public IEnumerable<StationStaff> Assignments { get; set; } = new List<StationStaff>();

    public List<StationShiftCoverage> ShiftCoverageByStation { get; set; } = new();
    public int TotalEmptyShifts { get; set; }
    public List<StationStaff> CurrentlyWorkingStaff { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public bool IsEditMode => Input?.StaffId > 0;

    public async Task<IActionResult> OnGetAsync(int? staffId)
    {
        await LoadDataAsync();

        if (staffId.HasValue)
        {
            var assignment = await _staffService.GetAssignmentAsync(staffId.Value);
            if (assignment != null)
            {
                Input = new StaffAssignmentInput
                {
                    StaffId = assignment.StaffId,
                    UserId = assignment.UserId,
                    StationId = assignment.StationId,
                    ShiftStart = assignment.ShiftStart.ToString(@"hh\:mm"),
                    ShiftEnd = assignment.ShiftEnd.ToString(@"hh\:mm"),
                    AssignedAt = assignment.AssignedAt,
                    IsActive = assignment.IsActive
                };
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAssignStaffAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var assignment = new StationStaff
            {
                StaffId = Input.StaffId,
                UserId = Input.UserId,
                StationId = Input.StationId,
                ShiftStart = TimeSpan.Parse(Input.ShiftStart),
                ShiftEnd = TimeSpan.Parse(Input.ShiftEnd),
                AssignedAt = Input.AssignedAt ?? DateTime.UtcNow,
                IsActive = Input.IsActive
            };

            _logger.LogInformation(
                "[StaffSchedule] Tạo phân ca: UserId={UserId}, StationId={StationId}, Ca={ShiftStart}-{ShiftEnd}",
                assignment.UserId, assignment.StationId, assignment.ShiftStart, assignment.ShiftEnd);

            if (IsEditMode)
            {
                await _staffService.UpdateAssignmentAsync(assignment);
                SuccessMessage = "Cập nhật phân ca thành công";
                await NotifyStaffAssignmentChange("cập nhật", assignment);
            }
            else
            {
                await _staffService.AssignStaffAsync(assignment);
                SuccessMessage = "Phân ca nhân viên thành công";
                await NotifyStaffAssignmentChange("tạo mới", assignment);
            }

            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRemoveAssignmentAsync(int staffId)
    {
        try
        {
            var assignment = await _staffService.GetAssignmentAsync(staffId);
            await _staffService.RemoveAssignmentAsync(staffId);
            
            SuccessMessage = "Xóa phân ca thành công";

            if (assignment != null)
            {
                await NotifyStaffAssignmentChange("xóa", assignment);
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDataAsync();
            return Page();
        }
    }

    private async Task LoadDataAsync()
    {
        AvailableStaff = await _userService.GetUsersByRoleAsync(BusinessObjects.Enums.UserRole.StationStaff);
        Stations = await _stationService.GetStationsAsync();
        
        // ✅ Sắp xếp hợp lý
        Assignments = (await _staffService.GetAssignmentsAsync())
            .OrderByDescending(a => a.IsActive)
            .ThenByDescending(a => a.IsCurrentlyWorking)
            .ThenBy(a => a.Station?.Name)
            .ThenBy(a => a.ShiftStart)
            .ToList();
        
        await LoadShiftCoverageAsync();
        await LoadCurrentlyWorkingStaffAsync();
    }

    // ✅ FIX: Logic phân ca ĐÚNG
    private async Task LoadShiftCoverageAsync()
    {
        var morningStart = new TimeSpan(6, 0, 0);
        var afternoonStart = new TimeSpan(14, 0, 0);
        var eveningEnd = new TimeSpan(22, 0, 0);

        ShiftCoverageByStation = new List<StationShiftCoverage>();
        TotalEmptyShifts = 0;

        foreach (var station in Stations)
        {
            var stationAssignments = Assignments
                .Where(a => a.StationId == station.StationId && a.IsActive)
                .ToList();

            // ✅ LOGIC MỚI: Phân loại dựa trên giờ bắt đầu ca
            var morningStaff = stationAssignments
                .Where(a => a.ShiftStart >= morningStart && a.ShiftStart < afternoonStart)
                .ToList();

            var afternoonStaff = stationAssignments
                .Where(a => a.ShiftStart >= afternoonStart && a.ShiftStart < eveningEnd)
                .ToList();

            var coverage = new StationShiftCoverage
            {
                StationId = station.StationId,
                StationName = station.Name,
                StationAddress = station.Address,
                MorningShiftStaff = morningStaff,
                AfternoonShiftStaff = afternoonStaff,
                HasMorningCoverage = morningStaff.Any(),
                HasAfternoonCoverage = afternoonStaff.Any()
            };

            if (!coverage.HasMorningCoverage) TotalEmptyShifts++;
            if (!coverage.HasAfternoonCoverage) TotalEmptyShifts++;

            ShiftCoverageByStation.Add(coverage);
        }
    }

    private async Task LoadCurrentlyWorkingStaffAsync()
    {
        CurrentlyWorkingStaff = new List<StationStaff>();
        
        foreach (var assignment in Assignments.Where(a => a.IsActive))
        {
            var isWorking = await _staffService.IsStaffCurrentlyWorkingAsync(assignment.UserId);
            if (isWorking)
            {
                CurrentlyWorkingStaff.Add(assignment);
            }
        }
    }

    private async Task NotifyStaffAssignmentChange(string action, StationStaff assignment)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(assignment.UserId);
            var station = await _stationService.GetStationAsync(assignment.StationId);

            var message = action switch
            {
                "tạo mới" => $"Phân ca mới: {user?.FullName} → {station?.Name}",
                "cập nhật" => $"Cập nhật phân ca: {user?.FullName} tại {station?.Name}",
                "xóa" => $"Xóa phân ca: {user?.FullName} khỏi {station?.Name}",
                _ => "Phân ca đã thay đổi"
            };

            await _hubContext.Clients.Group("Admin").SendAsync("StaffAssignmentChanged", new
            {
                action,
                staffId = assignment.StaffId,
                userId = assignment.UserId,
                userName = user?.FullName,
                stationId = assignment.StationId,
                stationName = station?.Name,
                shiftStart = assignment.ShiftStart.ToString(@"hh\:mm"),
                shiftEnd = assignment.ShiftEnd.ToString(@"hh\:mm"),
                isActive = assignment.IsActive,
                message,
                timestamp = DateTime.UtcNow
            });

            await _hubContext.Clients.User(assignment.UserId.ToString()).SendAsync("ReceiveNotification", new
            {
                message = $"Phân ca của bạn đã được {action}",
                type = "info",
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation(
                "[SignalR] Phân ca {Action}: StaffId={StaffId}, Nhân viên={UserName}, Trạm={StationName}",
                action, assignment.StaffId, user?.FullName, station?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SignalR] Lỗi gửi thông báo phân ca");
        }
    }

    public class StationShiftCoverage
    {
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string StationAddress { get; set; } = string.Empty;
        public List<StationStaff> MorningShiftStaff { get; set; } = new();
        public List<StationStaff> AfternoonShiftStaff { get; set; } = new();
        public bool HasMorningCoverage { get; set; }
        public bool HasAfternoonCoverage { get; set; }
        
        public bool HasAnyCoverage => HasMorningCoverage || HasAfternoonCoverage;
        public bool IsFullyCovered => HasMorningCoverage && HasAfternoonCoverage;
        public int EmptyShiftCount => (HasMorningCoverage ? 0 : 1) + (HasAfternoonCoverage ? 0 : 1);
    }

    public class StaffAssignmentInput
    {
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhân viên")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạm")]
        public int StationId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giờ bắt đầu ca")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Định dạng giờ không hợp lệ")]
        public string ShiftStart { get; set; } = "08:00";

        [Required(ErrorMessage = "Vui lòng nhập giờ kết thúc ca")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Định dạng giờ không hợp lệ")]
        public string ShiftEnd { get; set; } = "17:00";

        public DateTime? AssignedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BSMS.WebApp.Pages.Admin.Operations;

[Authorize(Roles = "Admin")]
public class StaffScheduleModel : BasePageModel
{
    private readonly IStationStaffService _staffService;
    private readonly IChangingStationService _stationService;
    private readonly IUserService _userService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<StaffScheduleModel>? _logger;

    public StaffScheduleModel(
        IStationStaffService staffService,
        IChangingStationService stationService,
        IUserService userService,
        IPasswordHasher passwordHasher,
        IUserActivityLogService activityLogService,
        ILogger<StaffScheduleModel>? logger = null) : base(activityLogService)
    {
        _staffService = staffService;
        _stationService = stationService;
        _userService = userService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    // Properties
    public List<StationStaff> Assignments { get; set; } = new();
    public List<ChangingStation> Stations { get; set; } = new();
    public List<User> AvailableStaff { get; set; } = new();
    public List<StationStaff> CurrentlyWorkingStaff { get; set; } = new();
    public List<ShiftCoverageViewModel> ShiftCoverageByStation { get; set; } = new();
    public int TotalEmptyShifts { get; set; }
    public bool IsEditMode { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public StaffAssignmentInputModel Input { get; set; } = new();

    [BindProperty]
    public NewStaffInputModel NewStaffInput { get; set; } = new();

    // OnGet
    public async Task OnGetAsync(int? staffId = null)
    {
        if (staffId.HasValue)
        {
            var assignment = await _staffService.GetAssignmentAsync(staffId.Value);
            if (assignment != null)
            {
                IsEditMode = true;
                Input = new StaffAssignmentInputModel
                {
                    StaffId = assignment.StaffId,
                    UserId = assignment.UserId,
                    StationId = assignment.StationId,
                    ShiftStart = assignment.ShiftStart.ToString(@"hh\:mm"),
                    ShiftEnd = assignment.ShiftEnd.ToString(@"hh\:mm"),
                    AssignedAt = assignment.AssignedAt.ToLocalTime(),
                    IsActive = assignment.IsActive
                };
            }
        }

        await LoadDataAsync();
    }


    // POST: Tạo nhân viên mới
    public async Task<IActionResult> OnPostCreateNewStaffAsync()
    {
        // ✅ XÓA TẤT CẢ validation errors của Input (form phân ca)
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Input.")).ToList())
        {
            ModelState.Remove(key);
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) })
                .ToList();

            _logger?.LogWarning("ModelState không hợp lệ: {Errors}", 
                string.Join(", ", errors.SelectMany(e => e.Errors ?? Enumerable.Empty<string>())));

            ErrorMessage = $"❌ Lỗi: {string.Join(", ", errors.SelectMany(e => e.Errors ?? Enumerable.Empty<string>()))}";
            
            await LoadDataAsync();
            return Page();
        }

        try
        {
            // 1. Kiểm tra username đã tồn tại
            if (await _userService.IsUsernameExistsAsync(NewStaffInput.Username.Trim()))
            {
                ErrorMessage = $"❌ Tên đăng nhập '{NewStaffInput.Username}' đã tồn tại.";
                await LoadDataAsync();
                return Page();
            }

            // 2. Kiểm tra email đã tồn tại
            if (!string.IsNullOrWhiteSpace(NewStaffInput.Email) &&
                await _userService.IsEmailExistsAsync(NewStaffInput.Email.Trim()))
            {
                ErrorMessage = $"❌ Email '{NewStaffInput.Email}' đã được sử dụng.";
                await LoadDataAsync();
                return Page();
            }

            // 3. Tạo User mới
            var newUser = new User
            {
                Username = NewStaffInput.Username.Trim(),
                FullName = NewStaffInput.FullName.Trim(),
                Email = NewStaffInput.Email?.Trim() ?? string.Empty,
                Phone = NewStaffInput.Phone?.Trim() ?? string.Empty,
                Role = UserRole.StationStaff,
                PasswordHash = _passwordHasher.HashPassword(NewStaffInput.Password),
                CreatedAt = DateTime.UtcNow
            };

            await _userService.CreateUserAsync(newUser);
            
            await LogActivityAsync("Staff", $"Created new staff user: {newUser.Username} ({newUser.FullName})");

            TempData["SuccessMessage"] = $"✅ Đã tạo nhân viên mới: {newUser.FullName} (Username: {newUser.Username})";

            // ✅ CHỈ REDIRECT, KHÔNG CẦN PHÂN CA
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating new staff user");
            ErrorMessage = $"❌ Lỗi khi tạo nhân viên: {ex.Message}";
            await LoadDataAsync();
            return Page();
        }
    }

    // POST: Assign Staff
    public async Task<IActionResult> OnPostAssignStaffAsync()
    {
        // ✅ VALIDATE THỦ CÔNG
        if (Input.UserId <= 0)
        {
            ModelState.AddModelError("Input.UserId", "Vui lòng chọn nhân viên");
        }
        
        if (Input.StationId <= 0)
        {
            ModelState.AddModelError("Input.StationId", "Vui lòng chọn trạm");
        }
        
        if (string.IsNullOrWhiteSpace(Input.ShiftStart))
        {
            ModelState.AddModelError("Input.ShiftStart", "Vui lòng nhập giờ bắt đầu");
        }
        
        if (string.IsNullOrWhiteSpace(Input.ShiftEnd))
        {
            ModelState.AddModelError("Input.ShiftEnd", "Vui lòng nhập giờ kết thúc");
        }

        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var shiftStart = TimeSpan.Parse(Input.ShiftStart);
            var shiftEnd = TimeSpan.Parse(Input.ShiftEnd);
            var assignedAt = Input.AssignedAt ?? DateTime.Now;

            var assignment = new StationStaff
            {
                StaffId = Input.StaffId,
                UserId = Input.UserId,
                StationId = Input.StationId,
                ShiftStart = shiftStart,
                ShiftEnd = shiftEnd,
                AssignedAt = assignedAt.ToUniversalTime(),
                IsActive = Input.IsActive
            };

            if (Input.StaffId == 0)
            {
                await _staffService.AssignStaffAsync(assignment);
                await LogActivityAsync("Staff", $"Assigned staff #{Input.UserId} to station #{Input.StationId}");
                TempData["SuccessMessage"] = "Đã phân công nhân viên thành công.";
            }
            else
            {
                await _staffService.UpdateAssignmentAsync(assignment);
                await LogActivityAsync("Staff", $"Updated assignment #{Input.StaffId}");
                TempData["SuccessMessage"] = "Đã cập nhật phân ca thành công.";
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync();
            return Page();
        }
    }

    // POST: Remove Assignment
    public async Task<IActionResult> OnPostRemoveAssignmentAsync(int staffId)
    {
        try
        {
            await _staffService.RemoveAssignmentAsync(staffId);
            await LogActivityAsync("Staff", $"Removed assignment #{staffId}");
            SuccessMessage = "Đã xóa phân ca thành công.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync();
            return Page();
        }
    }

    // Load Data
    private async Task LoadDataAsync()
    {
        Assignments = (await _staffService.GetAssignmentsAsync()).ToList();
        Stations = (await _stationService.GetStationsAsync()).ToList();
        AvailableStaff = (await _userService.GetUsersByRoleAsync(UserRole.StationStaff)).ToList();

        // Get currently working staff
        CurrentlyWorkingStaff = new List<StationStaff>();
        foreach (var assignment in Assignments)
        {
            if (await _staffService.IsStaffCurrentlyWorkingAsync(assignment.UserId))
            {
                CurrentlyWorkingStaff.Add(assignment);
            }
        }

        // Build shift coverage
        ShiftCoverageByStation = new List<ShiftCoverageViewModel>();
        TotalEmptyShifts = 0;

        foreach (var station in Stations)
        {
            var stationAssignments = Assignments.Where(a => a.StationId == station.StationId && a.IsActive).ToList();
            
            var morningShift = stationAssignments.Where(a => a.ShiftStart >= TimeSpan.FromHours(6) && a.ShiftStart < TimeSpan.FromHours(14)).ToList();
            var afternoonShift = stationAssignments.Where(a => a.ShiftStart >= TimeSpan.FromHours(14) && a.ShiftStart < TimeSpan.FromHours(22)).ToList();

            var hasMorning = morningShift.Any();
            var hasAfternoon = afternoonShift.Any();
            var emptyCount = (hasMorning ? 0 : 1) + (hasAfternoon ? 0 : 1);
            TotalEmptyShifts += emptyCount;

            ShiftCoverageByStation.Add(new ShiftCoverageViewModel
            {
                StationId = station.StationId,
                StationName = station.Name,
                StationAddress = station.Address,
                MorningShiftStaff = morningShift,
                AfternoonShiftStaff = afternoonShift,
                HasMorningCoverage = hasMorning,
                HasAfternoonCoverage = hasAfternoon,
                IsFullyCovered = hasMorning && hasAfternoon,
                HasAnyCoverage = hasMorning || hasAfternoon,
                EmptyShiftCount = emptyCount
            });
        }
    }

    // View Models
    public class StaffAssignmentInputModel
    {
        public int StaffId { get; set; }

        // ❌ BỎ [Required] vì sẽ conflict với form tạo nhân viên
        // [Required(ErrorMessage = "Vui lòng chọn nhân viên")]
        // [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhân viên")]
        public int UserId { get; set; }

        // ❌ BỎ [Required]
        // [Required(ErrorMessage = "Vui lòng chọn trạm")]
        // [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn trạm")]
        public int StationId { get; set; }

        // ❌ BỎ [Required]
        // [Required(ErrorMessage = "Vui lòng nhập giờ bắt đầu")]
        public string ShiftStart { get; set; } = "08:00";

        // ❌ BỎ [Required]
        // [Required(ErrorMessage = "Vui lòng nhập giờ kết thúc")]
        public string ShiftEnd { get; set; } = "17:00";

        public DateTime? AssignedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class NewStaffInputModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4-50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ cái, số và dấu gạch dưới")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại phải có 10-11 chữ số")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ShiftCoverageViewModel
    {
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string StationAddress { get; set; } = string.Empty;
        public List<StationStaff> MorningShiftStaff { get; set; } = new();
        public List<StationStaff> AfternoonShiftStaff { get; set; } = new();
        public bool HasMorningCoverage { get; set; }
        public bool HasAfternoonCoverage { get; set; }
        public bool IsFullyCovered { get; set; }
        public bool HasAnyCoverage { get; set; }
        public int EmptyShiftCount { get; set; }
    }
}

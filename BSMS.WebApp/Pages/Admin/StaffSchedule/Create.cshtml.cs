using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BSMS.WebApp.Pages.Admin.StaffSchedule;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IStationStaffService _staffService;
    private readonly IUserService _userService;
    private readonly IChangingStationService _stationService;

    public CreateModel(
        IStationStaffService staffService,
        IUserService userService,
        IChangingStationService stationService)
    {
        _staffService = staffService;
        _userService = userService;
        _stationService = stationService;
    }

    [BindProperty]
    public StationStaff StaffAssignment { get; set; } = new();

    public SelectList? AvailableStaff { get; set; }
    public SelectList? AvailableStations { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSelectListsAsync();
        
        // ✅ Set default values
        StaffAssignment.ShiftStart = TimeSpan.FromHours(8);
        StaffAssignment.ShiftEnd = TimeSpan.FromHours(17);
        StaffAssignment.AssignedAt = DateTime.Now; // ✅ Auto-fill current time
        StaffAssignment.IsActive = true;
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        try
        {
            // ✅ Ensure AssignedAt is set (fallback if client-side failed)
            if (StaffAssignment.AssignedAt == default || StaffAssignment.AssignedAt == DateTime.MinValue)
            {
                StaffAssignment.AssignedAt = DateTime.UtcNow;
            }
            
            await _staffService.AssignStaffAsync(StaffAssignment);
            TempData["SuccessMessage"] = "Phân công nhân viên thành công!";
            return RedirectToPage("./Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadSelectListsAsync();
            return Page();
        }
    }

    private async Task LoadSelectListsAsync()
    {
        // Get all users with StationStaff role
        var allUsers = await _userService.GetAllUsersAsync();
        var staffUsers = allUsers.Where(u => u.Role == UserRole.StationStaff).ToList();
        
        AvailableStaff = new SelectList(staffUsers, "UserId", "FullName");

        // Get all active stations
        var stations = await _stationService.GetStationsAsync();
        var activeStations = stations.Where(s => s.Status == StationStatus.Active).ToList();
        
        AvailableStations = new SelectList(activeStations, "StationId", "Name");
    }
}
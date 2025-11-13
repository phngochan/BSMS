using BSMS.BLL.Services;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSMS.WebApp.Pages.Admin.StaffSchedule;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IStationStaffService _staffService;

    public IndexModel(IStationStaffService staffService)
    {
        _staffService = staffService;
    }

    public IEnumerable<StationStaff> StaffAssignments { get; set; } = new List<StationStaff>();
    public Dictionary<int, bool> WorkingStatus { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        StaffAssignments = await _staffService.GetAssignmentsAsync();
        
        // Check working status for each staff
        foreach (var staff in StaffAssignments)
        {
            WorkingStatus[staff.StaffId] = await _staffService.IsStaffCurrentlyWorkingAsync(staff.UserId);
        }

        SuccessMessage = TempData["SuccessMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;
    }

    public async Task<IActionResult> OnPostDeleteAsync(int staffId)
    {
        try
        {
            await _staffService.RemoveAssignmentAsync(staffId);
            TempData["SuccessMessage"] = "Đã xóa phân công thành công.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int staffId, bool isActive)
    {
        try
        {
            var assignment = await _staffService.GetAssignmentAsync(staffId);
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phân công.";
                return RedirectToPage();
            }

            // ✅ FIX: Use the passed isActive value directly (it's already the NEW state)
            assignment.IsActive = isActive;
            await _staffService.UpdateAssignmentAsync(assignment);
            
            TempData["SuccessMessage"] = isActive 
                ? "Đã kích hoạt ca làm việc." 
                : "Đã tạm dừng ca làm việc.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }
}
using System.Collections.Generic;
using System.Linq;
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class SupportModel : BasePageModel
{
    private readonly ISupportService _supportService;

    public SupportModel(
        ISupportService supportService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _supportService = supportService;
    }

    public List<Support> SupportTickets { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSupportsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRateAsync(int supportId, int rating)
    {
        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Rating phải từ 1 đến 5.";
            await LoadSupportsAsync();
            return Page();
        }

        var support = await _supportService.GetSupportAsync(supportId);
        if (support == null || support.UserId != CurrentUserId)
        {
            TempData["ErrorMessage"] = "Yêu cầu không tồn tại hoặc không thuộc về bạn.";
            await LoadSupportsAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(support.StaffNote))
        {
            TempData["ErrorMessage"] = "Hiện chưa có phản hồi từ nhân viên trạm.";
            await LoadSupportsAsync();
            return Page();
        }

        await _supportService.UpdateSupportRatingAsync(supportId, rating);
        await LogActivityAsync("Support", $"Driver provided rating {rating} for ticket #{supportId}");
        TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi phản hồi.";
        return RedirectToPage();
    }

    private async Task LoadSupportsAsync()
    {
        if (CurrentUserId == 0)
        {
            SupportTickets = new();
            return;
        }

        SupportTickets = (await _supportService.GetSupportsByUserAsync(CurrentUserId)).ToList();
    }
}

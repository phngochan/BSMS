using BSMS.BLL.Services;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;

namespace BSMS.WebApp.ViewComponents;

public class ActiveStaffViewComponent : ViewComponent
{
    private readonly IStationStaffService _staffService;

    public ActiveStaffViewComponent(IStationStaffService staffService)
    {
        _staffService = staffService;
    }

    public async Task<IViewComponentResult> InvokeAsync(int stationId)
    {
        var activeStaff = await _staffService.GetStaffCurrentlyWorkingAtStationAsync(stationId);
        return View(activeStaff);
    }
}
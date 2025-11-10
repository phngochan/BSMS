using BSMS.BusinessObjects.Models;
using BSMS.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class FindStationsModel : PageModel
{
    private readonly IStationService _stationService;

    public FindStationsModel(IStationService stationService)
    {
        _stationService = stationService;
    }

    public string? SearchLocation { get; set; }
    public List<StationViewModel> Stations { get; set; } = new();

    public async Task OnGetAsync(string? location, double? lat, double? lng)
    {
        SearchLocation = location;

        if (lat.HasValue && lng.HasValue)
        {
            var nearby = await _stationService.GetNearbyStationsAsync(lat.Value, lng.Value);
            Stations = nearby.Select(s => new StationViewModel
            {
                StationId = s.StationId,
                Name = s.Name,
                Address = s.Address,
                Capacity = s.Capacity,
                AvailableBatteries = s.AvailableBatteries,
                Distance = s.Distance,
                Latitude = s.Latitude,
                Longitude = s.Longitude
            }).ToList();
            return;
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var stations = await _stationService.SearchStationsAsync(location);
            var avail = (await _stationService.GetStationsWithAvailabilityAsync())
                .ToDictionary(a => a.StationId, a => a.AvailableBatteries);

            Stations = stations.Select(s => new StationViewModel
            {
                StationId = s.StationId,
                Name = s.Name,
                Address = s.Address,
                Capacity = s.Capacity,
                AvailableBatteries = avail.TryGetValue(s.StationId, out var count) ? count : 0,
                Distance = 0,
                Latitude = s.Latitude,
                Longitude = s.Longitude
            }).OrderBy(s => s.Name).ToList();
            return;
        }

        // Default: show all active with availability
        var withAvail = await _stationService.GetStationsWithAvailabilityAsync();
        Stations = withAvail.Select(s => new StationViewModel
        {
            StationId = s.StationId,
            Name = s.Name,
            Address = s.Address,
            Capacity = s.Capacity,
            AvailableBatteries = s.AvailableBatteries,
            Distance = 0,
            Latitude = s.Latitude,
            Longitude = s.Longitude
        }).OrderBy(s => s.Name).ToList();
    }
}

public class StationViewModel
{
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int AvailableBatteries { get; set; }
    public double Distance { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

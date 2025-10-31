using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSMS.WebApp.Pages.Staff;

public class IndexModel : PageModel
{
    public int FullCount { get; set; }
    public int ChargingCount { get; set; }
    public int MaintenanceCount { get; set; }

    public List<BatteryRow> Batteries { get; set; } = new();

    public void OnGet()
    {
        // Demo data – replace with service layer calls when available
        Batteries = new List<BatteryRow>
        {
            new("BAT-001","Panasonic X1","2.3 kWh", 96, "Full", DateTime.Now.AddMinutes(-10)),
            new("BAT-014","LG Chem S2","2.0 kWh", 88, "Charging", DateTime.Now.AddMinutes(-3)),
            new("BAT-022","CATL E3","2.4 kWh", 72, "Maintenance", DateTime.Now.AddHours(-1)),
            new("BAT-035","BYD B1","2.1 kWh", 90, "Full", DateTime.Now.AddMinutes(-25)),
            new("BAT-041","Samsung SDI M","2.2 kWh", 84, "Charging", DateTime.Now.AddMinutes(-8)),
        };

        FullCount = Batteries.Count(b => b.Status == "Full");
        ChargingCount = Batteries.Count(b => b.Status == "Charging");
        MaintenanceCount = Batteries.Count(b => b.Status == "Maintenance");
    }
}

public record BatteryRow(
    string BatteryId,
    string Model,
    string Capacity,
    int Soh,
    string Status,
    DateTime LastUpdated
);

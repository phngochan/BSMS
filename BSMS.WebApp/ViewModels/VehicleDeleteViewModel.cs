namespace BSMS.WebApp.ViewModels;

public class VehicleDeleteViewModel
{
    public int UserId { get; set; }
    public int VehicleId { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string BatteryModel { get; set; } = string.Empty;
    public string BatteryType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}


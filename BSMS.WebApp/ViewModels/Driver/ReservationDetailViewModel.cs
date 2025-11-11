namespace BSMS.WebApp.ViewModels.Driver;

public class ReservationDetailViewModel
{
    public int ReservationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;
    public double StationLatitude { get; set; }
    public double StationLongitude { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public string VehicleVin { get; set; } = string.Empty;
    public string? BatteryModel { get; set; }
    public string BatteryType { get; set; } = string.Empty;

    public int? BatteryId { get; set; }

    public bool CanCancel { get; set; }
}


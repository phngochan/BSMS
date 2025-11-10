namespace BSMS.WebApp.ViewModels.Driver;

public class ReservationViewModel
{
    public int ReservationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;
    public double StationLatitude { get; set; }
    public double StationLongitude { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}


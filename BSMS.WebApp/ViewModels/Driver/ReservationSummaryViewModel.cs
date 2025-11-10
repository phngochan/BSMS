namespace BSMS.WebApp.ViewModels.Driver;

public class ReservationSummaryViewModel
{
    public int ReservationId { get; set; }
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
}


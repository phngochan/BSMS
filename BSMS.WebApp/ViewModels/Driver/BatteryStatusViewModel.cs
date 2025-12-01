namespace BSMS.WebApp.ViewModels.Driver;

public class BatteryStatusViewModel
{
    public int Percent { get; set; }
    public int EstimatedRange { get; set; }
    public DateTime LastSwapped { get; set; }
}


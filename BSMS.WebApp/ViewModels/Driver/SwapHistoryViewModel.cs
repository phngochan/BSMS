namespace BSMS.WebApp.ViewModels.Driver;

public class SwapHistoryViewModel
{
    public int TransactionId { get; set; }
    public DateTime SwapTime { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string BatteryTakenModel { get; set; } = string.Empty;
    public string BatteryReturnedModel { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public string Duration { get; set; } = string.Empty;
}


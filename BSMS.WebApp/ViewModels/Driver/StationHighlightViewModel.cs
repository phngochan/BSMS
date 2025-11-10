namespace BSMS.WebApp.ViewModels.Driver;

public class StationHighlightViewModel
{
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int AvailableBatteries { get; set; }
    public int Capacity { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Distance { get; set; }
}


using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class Battery
{
    public int BatteryId { get; set; }
    public string Model { get; set; }
    public int Capacity { get; set; }
    public decimal Soh { get; set; }
    public BatteryStatus Status { get; set; } = BatteryStatus.Full;
    public int StationId { get; set; }
    public DateTime? LastMaintenance { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ChangingStation Station { get; set; }
}

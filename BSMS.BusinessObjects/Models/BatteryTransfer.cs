using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class BatteryTransfer
{
    public int TransferId { get; set; }
    public int BatteryId { get; set; }
    public int FromStationId { get; set; }
    public int ToStationId { get; set; }
    public DateTime TransferTime { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.InProgress;

    public Battery Battery { get; set; }
    public ChangingStation FromStation { get; set; }
    public ChangingStation ToStation { get; set; }
}

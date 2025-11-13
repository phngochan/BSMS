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
    public string? RejectionReason { get; set; }  
    public DateTime? CompletedAt { get; set; }    
    public int? ConfirmedByUserId { get; set; }   

    public Battery Battery { get; set; }
    public ChangingStation FromStation { get; set; }
    public ChangingStation ToStation { get; set; }
    public User? ConfirmedByUser { get; set; }   
}

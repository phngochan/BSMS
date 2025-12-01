using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class SwapTransaction
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public int VehicleId { get; set; }
    public int StationId { get; set; }
    public int BatteryTakenId { get; set; }
    public int? BatteryReturnedId { get; set; }
    public int? PaymentId { get; set; }
    public DateTime SwapTime { get; set; }
    public decimal TotalCost { get; set; }
    public SwapStatus Status { get; set; } = SwapStatus.Pending;

    public User User { get; set; }
    public Vehicle Vehicle { get; set; }
    public ChangingStation Station { get; set; }
    public Battery BatteryTaken { get; set; }
    public Battery? BatteryReturned { get; set; }
    public Payment? Payment { get; set; }
}

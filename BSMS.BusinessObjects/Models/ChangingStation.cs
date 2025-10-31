using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class ChangingStation
{
    public int StationId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Capacity { get; set; }
    public StationStatus Status { get; set; } = StationStatus.Active;
    public DateTime CreatedAt { get; set; }

    public ICollection<Battery> Batteries { get; set; }
    public ICollection<SwapTransaction> SwapTransactions { get; set; }
    public ICollection<Reservation> Reservations { get; set; }
    public ICollection<Support> Supports { get; set; }
    public ICollection<StationStaff> StationStaffs { get; set; }
    public ICollection<StationStatistics> StationStatistics { get; set; }
    public ICollection<BatteryTransfer> FromTransfers { get; set; }
    public ICollection<BatteryTransfer> ToTransfers { get; set; }
    public ICollection<Alert> Alerts { get; set; }
}
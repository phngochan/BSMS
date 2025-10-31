using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class Reservation
{
    public int ReservationId { get; set; }
    public int UserId { get; set; }
    public int VehicleId { get; set; }
    public int StationId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
    public Vehicle Vehicle { get; set; }
    public ChangingStation Station { get; set; }
}

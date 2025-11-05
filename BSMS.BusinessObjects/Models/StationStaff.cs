namespace BSMS.BusinessObjects.Models;
public class StationStaff
{
    public int StaffId { get; set; }
    public int UserId { get; set; }
    public int StationId { get; set; }
    public DateTime AssignedAt { get; set; }

    public User User { get; set; }
    public ChangingStation Station { get; set; }
}
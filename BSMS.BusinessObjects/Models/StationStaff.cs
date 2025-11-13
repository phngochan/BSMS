namespace BSMS.BusinessObjects.Models;
public class StationStaff
{
    public int StaffId { get; set; }
    public int UserId { get; set; }
    public int StationId { get; set; }
    public DateTime AssignedAt { get; set; }
    
    public TimeSpan ShiftStart { get; set; } = TimeSpan.FromHours(8); // Mặc định 08:00
    public TimeSpan ShiftEnd { get; set; } = TimeSpan.FromHours(17); // Mặc định 17:00
    public bool IsActive { get; set; } = true; // Trạng thái đang làm việc

    public User User { get; set; }
    public ChangingStation Station { get; set; }
}
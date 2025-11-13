using System.ComponentModel.DataAnnotations.Schema;

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

    // ✅ Computed properties
    [NotMapped]
    public bool IsCurrentlyWorking
    {
        get
        {
            if (!IsActive) return false;
            
            var currentTime = DateTime.Now.TimeOfDay;
            return currentTime >= ShiftStart && currentTime <= ShiftEnd;
        }
    }

    [NotMapped]
    public string ShiftTimeDisplay => $"{ShiftStart:hh\\:mm} - {ShiftEnd:hh\\:mm}";

    [NotMapped]
    public TimeSpan? TimeRemainingInShift
    {
        get
        {
            if (!IsCurrentlyWorking) return null;
            
            var currentTime = DateTime.Now.TimeOfDay;
            return ShiftEnd - currentTime;
        }
    }

    [NotMapped]
    public TimeSpan ShiftDuration => ShiftEnd - ShiftStart;

    [NotMapped]
    public string WorkingStatusDisplay => IsCurrentlyWorking 
        ? "Đang làm việc" 
        : IsActive 
            ? "Ngoài giờ" 
            : "Không hoạt động";
}
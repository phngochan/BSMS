using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Driver;
    public DateTime CreatedAt { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; }
    public ICollection<SwapTransaction> SwapTransactions { get; set; }
    public ICollection<Reservation> Reservations { get; set; }
    public ICollection<Payment> Payments { get; set; }
    public ICollection<Support> Supports { get; set; }
    public ICollection<UserPackage> UserPackages { get; set; }
    public ICollection<StationStaff> StationStaffs { get; set; }
}

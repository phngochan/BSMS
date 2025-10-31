using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class UserPackage
{
    public int UserPackageId { get; set; }
    public int UserId { get; set; }
    public int PackageId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PackageStatus Status { get; set; } = PackageStatus.Active;

    public User User { get; set; }
    public BatteryServicePackage Package { get; set; }
}


namespace BSMS.BusinessObjects.Models;
public class BatteryServicePackage
{
    public int PackageId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public string Description { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<UserPackage> UserPackages { get; set; }
}

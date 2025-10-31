namespace BSMS.BusinessObjects.Models;
public class Vehicle
{
    public int VehicleId { get; set; }
    public int UserId { get; set; }
    public string Vin { get; set; }
    public string BatteryModel { get; set; }
    public string BatteryType { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
}

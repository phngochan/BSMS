using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class Alert
{
    public int AlertId { get; set; }
    public int StationId { get; set; }
    public int BatteryId { get; set; }
    public AlertType AlertType { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Resolved { get; set; } = false;

    public ChangingStation Station { get; set; }
    public Battery Battery { get; set; }
}

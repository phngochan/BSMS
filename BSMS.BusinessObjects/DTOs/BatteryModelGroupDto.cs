namespace BSMS.BusinessObjects.DTOs;

public class BatteryModelGroupDto
{
    public string Model { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int TotalCount { get; set; }
    public int AvailableCount { get; set; }
    public int ChargingCount { get; set; }
    public int MaintenanceCount { get; set; }
}


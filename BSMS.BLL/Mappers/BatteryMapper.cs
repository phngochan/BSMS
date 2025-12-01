using BSMS.BusinessObjects.DTOs;

namespace BSMS.BLL.Mappers;

public static class BatteryMapper
{
    public static BatteryModelGroupDto ToBatteryModelGroupDto(
        string model,
        int capacity,
        int totalCount,
        int availableCount,
        int chargingCount,
        int maintenanceCount)
    {
        return new BatteryModelGroupDto
        {
            Model = model,
            Capacity = capacity,
            TotalCount = totalCount,
            AvailableCount = availableCount,
            ChargingCount = chargingCount,
            MaintenanceCount = maintenanceCount
        };
    }
}


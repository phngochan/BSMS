using BSMS.BusinessObjects.DTOs;
using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Mappers;

public static class StationMapper
{
    public static StationWithDistanceDto ToStationWithDistanceDto(
        int stationId,
        string name,
        string address,
        double latitude,
        double longitude,
        int capacity,
        string status,
        int availableBatteries,
        double distance)
    {
        return new StationWithDistanceDto
        {
            StationId = stationId,
            Name = name,
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            Capacity = capacity,
            Status = status,
            AvailableBatteries = availableBatteries,
            Distance = distance
        };
    }

    public static StationWithAvailabilityDto ToStationWithAvailabilityDto(
        int stationId,
        string name,
        string address,
        double latitude,
        double longitude,
        int capacity,
        string status,
        int availableBatteries)
    {
        return new StationWithAvailabilityDto
        {
            StationId = stationId,
            Name = name,
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            Capacity = capacity,
            Status = status,
            AvailableBatteries = availableBatteries
        };
    }
}


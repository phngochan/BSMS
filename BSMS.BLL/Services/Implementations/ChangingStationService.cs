using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class ChangingStationService : IChangingStationService
{
    private readonly IChangingStationRepository _stationRepository;

    public ChangingStationService(IChangingStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public async Task<ChangingStation> CreateStationAsync(ChangingStation station)
    {
        station.CreatedAt = DateTime.UtcNow;
        return await _stationRepository.CreateAsync(station);
    }

    public async Task DeleteStationAsync(int stationId)
    {
        var existing = await _stationRepository.GetSingleAsync(s => s.StationId == stationId);
        if (existing == null)
        {
            throw new InvalidOperationException("Station not found");
        }

        await _stationRepository.DeleteAsync(existing);
    }

    public async Task<ChangingStation?> GetStationAsync(int stationId, bool includeDetails = false)
    {
        return includeDetails
            ? await _stationRepository.GetStationWithDetailsAsync(stationId)
            : await _stationRepository.GetSingleAsync(s => s.StationId == stationId);
    }

    public async Task<int> GetStationCountAsync(StationStatus? status = null)
    {
        return await _stationRepository.CountByStatusAsync(status);
    }

    public async Task<IEnumerable<ChangingStation>> GetStationsAsync()
    {
        return await _stationRepository.GetAllAsync(orderBy: q => q.OrderBy(s => s.Name));
    }

    public async Task<IEnumerable<ChangingStation>> GetStationsWithDetailsAsync()
    {
        return await _stationRepository.GetStationsWithDetailsAsync();
    }

    public async Task UpdateStationAsync(ChangingStation station)
    {
        var existing = await _stationRepository.GetSingleAsync(s => s.StationId == station.StationId);
        if (existing == null)
        {
            throw new InvalidOperationException("Station not found");
        }

        existing.Name = station.Name;
        existing.Address = station.Address;
        existing.Latitude = station.Latitude;
        existing.Longitude = station.Longitude;
        existing.Capacity = station.Capacity;
        existing.Status = station.Status;

        await _stationRepository.UpdateAsync(existing);
    }
}

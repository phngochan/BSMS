using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IChangingStationRepository : IGenericRepository<ChangingStation>
{
    Task<IEnumerable<ChangingStation>> GetStationsWithDetailsAsync();
    Task<ChangingStation?> GetStationWithDetailsAsync(int stationId);
    Task<int> CountByStatusAsync(StationStatus? status = null);
}

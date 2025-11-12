using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface ISupportRepository : IGenericRepository<Support>
{
    Task<IEnumerable<Support>> GetOpenSupportsAsync();
    Task<IEnumerable<Support>> GetSupportsByStationAsync(int stationId);
    Task<Support?> GetSupportWithDetailsAsync(int supportId);
    Task<int> CountByStatusAsync(SupportStatus status);
    Task<IEnumerable<Support>> GetSupportsByUserAsync(int userId);
}

using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface ISupportService
{
    Task<IEnumerable<Support>> GetOpenSupportsAsync();
    Task<IEnumerable<Support>> GetSupportsByStationAsync(int stationId);
    Task<Support?> GetSupportAsync(int supportId);
    Task<Support> CreateSupportAsync(Support support);
    Task UpdateSupportAsync(Support support);
    Task UpdateSupportStatusAsync(int supportId, SupportStatus status, int? rating = null);
    Task DeleteSupportAsync(int supportId);
    Task<int> CountByStatusAsync(SupportStatus status);
}

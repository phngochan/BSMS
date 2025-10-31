using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IAlertRepository : IGenericRepository<Alert>
{
    Task<IEnumerable<Alert>> GetUnresolvedAlertsAsync();
    Task<IEnumerable<Alert>> GetAlertsByUserRoleAsync(string role, int? userId = null);
    Task<int> GetUnreadCountByUserAsync(int userId);
    Task MarkAsResolvedAsync(int alertId);
}

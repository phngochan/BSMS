using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IAlertService
{
    Task<IEnumerable<Alert>> GetUnresolvedAlertsAsync();
    Task<IEnumerable<Alert>> GetAlertsForUserAsync(int userId, string role);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsResolvedAsync(int alertId);
    Task CreateAlertAsync(Alert alert);
}

using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepository;

    public AlertService(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<IEnumerable<Alert>> GetUnresolvedAlertsAsync()
    {
        return await _alertRepository.GetUnresolvedAlertsAsync();
    }

    public async Task<IEnumerable<Alert>> GetAlertsForUserAsync(int userId, string role)
    {
        return await _alertRepository.GetAlertsByUserRoleAsync(role, userId);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _alertRepository.GetUnreadCountByUserAsync(userId);
    }

    public async Task MarkAsResolvedAsync(int alertId)
    {
        await _alertRepository.MarkAsResolvedAsync(alertId);
    }

    public async Task CreateAlertAsync(Alert alert)
    {
        alert.CreatedAt = DateTime.UtcNow;
        alert.Resolved = false;
        
        await _alertRepository.CreateAsync(alert);
        
        // Note: SignalR notification broadcasting should be done in PageModel/Controller layer
    }
}

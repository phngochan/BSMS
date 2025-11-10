using BSMS.BusinessObjects.Enums;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services.Implementations
{
    public class PackageExpirationService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PackageExpirationService> _logger;

        public PackageExpirationService(IServiceProvider services, ILogger<PackageExpirationService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IUserPackageRepository>();
                    var expired = await repo.GetExpiredAsync();

                    foreach (var pkg in expired)
                    {
                        pkg.Status = PackageStatus.Expired;
                        await repo.UpdateAsync(pkg);
                        _logger.LogInformation($"Gói {pkg.UserPackageId} đã hết hạn.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi kiểm tra gói hết hạn");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}

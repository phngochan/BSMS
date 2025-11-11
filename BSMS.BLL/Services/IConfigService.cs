using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IConfigService
{
    Task<IEnumerable<Config>> GetAllAsync();
    Task<Config?> GetAsync(int configId);
    Task<Config?> GetByNameAsync(string name);
    Task<Config> SaveAsync(Config config);
    Task DeleteAsync(int configId);
}

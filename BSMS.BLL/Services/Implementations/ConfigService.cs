using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class ConfigService : IConfigService
{
    private readonly IConfigRepository _configRepository;

    public ConfigService(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    public async Task DeleteAsync(int configId)
    {
        var existing = await _configRepository.GetSingleAsync(c => c.ConfigId == configId);
        if (existing == null)
        {
            throw new InvalidOperationException("Config not found");
        }

        await _configRepository.DeleteAsync(existing);
    }

    public async Task<IEnumerable<Config>> GetAllAsync()
    {
        return await _configRepository.GetAllAsync(orderBy: q => q.OrderBy(c => c.Name));
    }

    public async Task<Config?> GetAsync(int configId)
    {
        return await _configRepository.GetSingleAsync(c => c.ConfigId == configId);
    }

    public async Task<Config?> GetByNameAsync(string name)
    {
        return await _configRepository.GetByNameAsync(name);
    }

    public async Task<Config> SaveAsync(Config config)
    {
        if (config.ConfigId == 0)
        {
            return await _configRepository.CreateAsync(config);
        }

        var existing = await _configRepository.GetSingleAsync(c => c.ConfigId == config.ConfigId);
        if (existing == null)
        {
            throw new InvalidOperationException("Config not found");
        }

        existing.Name = config.Name;
        existing.Value = config.Value;
        existing.Description = config.Description;

        await _configRepository.UpdateAsync(existing);
        return existing;
    }
}

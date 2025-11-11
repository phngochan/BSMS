using System;
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
        await EnsureUniqueNameAsync(config);
        ValidateConfigValue(config);

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

    private async Task EnsureUniqueNameAsync(Config config)
    {
        var existingByName = await _configRepository.GetByNameAsync(config.Name);
        if (existingByName != null && existingByName.ConfigId != config.ConfigId)
        {
            throw new InvalidOperationException("Tên cấu hình đã tồn tại.");
        }
    }

    private static void ValidateConfigValue(Config config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
        {
            return;
        }

        switch (config.Name.Trim().ToLowerInvariant())
        {
            case "min_full_percent":
                if (!int.TryParse(config.Value, out var percent) || percent < 0 || percent > 100)
                {
                    throw new InvalidOperationException("min_full_percent phải nằm trong khoảng 0 - 100.");
                }
                break;

            case "max_inactive_hours":
                if (!int.TryParse(config.Value, out var hours) || hours <= 0)
                {
                    throw new InvalidOperationException("max_inactive_hours phải lớn hơn 0.");
                }
                break;
        }
    }
}

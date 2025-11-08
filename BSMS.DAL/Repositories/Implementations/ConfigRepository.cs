using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class ConfigRepository : GenericRepository<Config>, IConfigRepository
{
    public ConfigRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<Config?> GetByNameAsync(string name)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name);
    }
}

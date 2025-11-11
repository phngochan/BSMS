using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(BSMSDbContext context) : base(context) { }

    public async Task<IEnumerable<Vehicle>> GetVehiclesByUserIdAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task<Vehicle?> GetVehicleByVinAsync(string vin)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Vin == vin);
    }

    public async Task<bool> IsVinExistsAsync(string vin, int? excludeVehicleId = null)
    {
        var query = _dbSet.Where(v => v.Vin == vin);
        
        if (excludeVehicleId.HasValue)
        {
            query = query.Where(v => v.VehicleId != excludeVehicleId.Value);
        }
        
        return await query.AnyAsync();
    }
}


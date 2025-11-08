using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class BatteryTransferRepository : GenericRepository<BatteryTransfer>, IBatteryTransferRepository
{
    public BatteryTransferRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<BatteryTransfer>> GetRecentTransfersAsync(int count = 20)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.Battery)
            .Include(t => t.FromStation)
            .Include(t => t.ToStation)
            .OrderByDescending(t => t.TransferTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<BatteryTransfer>> GetTransfersByStationAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.FromStationId == stationId || t.ToStationId == stationId)
            .Include(t => t.Battery)
            .Include(t => t.FromStation)
            .Include(t => t.ToStation)
            .OrderByDescending(t => t.TransferTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<BatteryTransfer>> GetTransfersInProgressAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Status == TransferStatus.InProgress)
            .Include(t => t.Battery)
            .Include(t => t.FromStation)
            .Include(t => t.ToStation)
            .OrderBy(t => t.TransferTime)
            .ToListAsync();
    }

    public async Task<BatteryTransfer?> GetTransferWithDetailsAsync(int transferId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.Battery)
            .Include(t => t.FromStation)
            .Include(t => t.ToStation)
            .FirstOrDefaultAsync(t => t.TransferId == transferId);
    }
}

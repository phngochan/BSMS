using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class SwapTransactionRepository : GenericRepository<SwapTransaction>, ISwapTransactionRepository
{
    public SwapTransactionRepository(BSMSDbContext context) : base(context) { }

    public async Task<IEnumerable<SwapTransaction>> GetTransactionsByUserIdAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(st => st.Station)
            .Include(st => st.BatteryTaken)
            .Include(st => st.BatteryReturned)
            .Where(st => st.UserId == userId)
            .OrderByDescending(st => st.SwapTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<SwapTransaction>> GetTransactionsByStationIdAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(st => st.User)
            .Include(st => st.BatteryTaken)
            .Include(st => st.BatteryReturned)
            .Where(st => st.StationId == stationId)
            .OrderByDescending(st => st.SwapTime)
            .ToListAsync();
    }

    public async Task<SwapTransaction?> GetTransactionWithDetailsAsync(int transactionId)
    {
        return await _dbSet
            .Include(st => st.User)
            .Include(st => st.Vehicle)
            .Include(st => st.Station)
            .Include(st => st.BatteryTaken)
            .Include(st => st.BatteryReturned)
            .Include(st => st.Payment)
            .FirstOrDefaultAsync(st => st.TransactionId == transactionId);
    }

    public async Task<SwapTransaction?> GetPendingTransactionAsync(int userId, int vehicleId, int stationId, int batteryTakenId)
    {
        return await _dbSet
            .Include(st => st.User)
            .Include(st => st.Vehicle)
            .Include(st => st.Station)
            .Include(st => st.BatteryTaken)
            .Include(st => st.BatteryReturned)
            .FirstOrDefaultAsync(st =>
                st.UserId == userId &&
                st.VehicleId == vehicleId &&
                st.StationId == stationId &&
                st.BatteryTakenId == batteryTakenId &&
                st.Status == SwapStatus.Pending);
    }
}


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
            .Include(st => st.Vehicle)
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

    public async Task<int> CountDailyTransactionsAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await _dbSet
            .AsNoTracking()
            .CountAsync(t => t.SwapTime >= start &&
                             t.SwapTime < end &&
                             t.Status == SwapStatus.Completed);
    }

    public async Task<decimal> GetRevenueForCurrentMonthAsync()
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1);

        return await _dbSet
            .AsNoTracking()
            .Where(t => t.SwapTime >= start &&
                        t.SwapTime < end &&
                        t.Status == SwapStatus.Completed)
            .SumAsync(t => t.TotalCost);
    }

    public async Task<IEnumerable<SwapTransaction>> GetRecentTransactionsAsync(int count = 25)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.Station)
            .Include(t => t.BatteryTaken)
            .Include(t => t.BatteryReturned)
            .OrderByDescending(t => t.SwapTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<SwapTransaction>> GetTransactionsByStationAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.StationId == stationId)
            .Include(t => t.User)
            .Include(t => t.Station)
            .Include(t => t.BatteryTaken)
            .Include(t => t.BatteryReturned)
            .OrderByDescending(t => t.SwapTime)
            .ToListAsync();
    }

    public async Task<SwapTransaction?> GetTransactionWithDetailsAsync(int transactionId)
    {
        return await _dbSet
        .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.Station)
            .Include(t => t.BatteryTaken)
            .Include(t => t.BatteryReturned)
            .Include(t => t.Vehicle)
            .Include(t => t.Payment)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
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

    public async Task<IDictionary<int, DateTime>> GetLatestCompletedSwapTimesAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Status == SwapStatus.Completed)
            .GroupBy(t => t.StationId)
            .Select(g => new
            {
                StationId = g.Key,
                LastSwap = g.Max(t => t.SwapTime)
            })
            .ToDictionaryAsync(k => k.StationId, v => v.LastSwap);
    }
}

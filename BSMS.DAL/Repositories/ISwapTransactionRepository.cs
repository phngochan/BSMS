
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface ISwapTransactionRepository : IGenericRepository<SwapTransaction>
{
    Task<IEnumerable<SwapTransaction>> GetTransactionsByUserIdAsync(int userId);
    Task<IEnumerable<SwapTransaction>> GetTransactionsByStationIdAsync(int stationId);
    Task<SwapTransaction?> GetTransactionWithDetailsAsync(int transactionId);
    Task<SwapTransaction?> GetPendingTransactionAsync(int userId, int vehicleId, int stationId, int batteryTakenId);
    Task<IEnumerable<SwapTransaction>> GetRecentTransactionsAsync(int count = 25);
    Task<IEnumerable<SwapTransaction>> GetTransactionsByStationAsync(int stationId);
    Task<decimal> GetRevenueForCurrentMonthAsync();
    Task<int> CountDailyTransactionsAsync(DateTime date);
    Task<IDictionary<int, DateTime>> GetLatestCompletedSwapTimesAsync();
}

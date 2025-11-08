using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface ISwapTransactionRepository : IGenericRepository<SwapTransaction>
{
    Task<IEnumerable<SwapTransaction>> GetRecentTransactionsAsync(int count = 25);
    Task<IEnumerable<SwapTransaction>> GetTransactionsByStationAsync(int stationId);
    Task<SwapTransaction?> GetTransactionWithDetailsAsync(int transactionId);
    Task<decimal> GetRevenueForCurrentMonthAsync();
    Task<int> CountDailyTransactionsAsync(DateTime date);
}

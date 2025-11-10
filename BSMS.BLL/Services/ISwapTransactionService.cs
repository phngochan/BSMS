using System.Collections.Generic;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface ISwapTransactionService
{
    Task<IEnumerable<SwapTransaction>> GetRecentTransactionsAsync(int count = 25);
    Task<IEnumerable<SwapTransaction>> GetTransactionsByStationAsync(int stationId);
    Task<SwapTransaction?> GetTransactionAsync(int transactionId);
    Task<SwapTransaction> CreateTransactionAsync(SwapTransaction transaction);
    Task UpdateTransactionAsync(SwapTransaction transaction);
    Task UpdateTransactionStatusAsync(int transactionId, SwapStatus status);
    Task DeleteTransactionAsync(int transactionId);
    Task<decimal> GetCurrentMonthRevenueAsync();
    Task<int> GetDailyTransactionCountAsync(DateTime date);
    Task<IDictionary<int, DateTime>> GetLatestCompletedSwapTimesAsync();
}

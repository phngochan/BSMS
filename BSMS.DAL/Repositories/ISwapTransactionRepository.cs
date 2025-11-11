using BSMS.BusinessObjects.Models;
using BSMS.BusinessObjects.Enums;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface ISwapTransactionRepository : IGenericRepository<SwapTransaction>
{
    Task<IEnumerable<SwapTransaction>> GetTransactionsByUserIdAsync(int userId);
    Task<IEnumerable<SwapTransaction>> GetTransactionsByStationIdAsync(int stationId);
    Task<SwapTransaction?> GetTransactionWithDetailsAsync(int transactionId);
    Task<SwapTransaction?> GetPendingTransactionAsync(int userId, int vehicleId, int stationId, int batteryTakenId);
}


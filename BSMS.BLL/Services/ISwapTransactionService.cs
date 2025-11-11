using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface ISwapTransactionService
{
    Task<SwapTransaction> CreatePendingSwapTransactionAsync(
        int userId,
        int vehicleId,
        int stationId,
        int batteryTakenId);
    
    Task<SwapTransaction> CompleteSwapTransactionAsync(
        int userId,
        int vehicleId,
        int stationId,
        int batteryTakenId,
        int batteryReturnedId,
        decimal totalCost,
        BusinessObjects.Enums.BatteryStatus returnedBatteryStatus = BusinessObjects.Enums.BatteryStatus.Charging);
    
    Task<bool> CancelSwapTransactionAsync(int userId, int vehicleId, int stationId, int batteryTakenId);
    
    Task<SwapTransaction?> GetTransactionByIdAsync(int transactionId);
    Task<SwapTransaction?> GetPendingTransactionAsync(int userId, int vehicleId, int stationId, int batteryTakenId);
    Task<IEnumerable<SwapTransaction>> GetUserTransactionsAsync(int userId);
    Task<IEnumerable<SwapTransaction>> GetStationTransactionsAsync(int stationId);
}


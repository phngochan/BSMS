using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class SwapTransactionService : ISwapTransactionService
{
    private readonly ISwapTransactionRepository _swapTransactionRepo;
    private readonly IBatteryRepository _batteryRepo;
    private readonly ILogger<SwapTransactionService> _logger;

    public SwapTransactionService(
        ISwapTransactionRepository swapTransactionRepo,
        IBatteryRepository batteryRepo,
        ILogger<SwapTransactionService> logger)
    {
        _swapTransactionRepo = swapTransactionRepo;
        _batteryRepo = batteryRepo;
        _logger = logger;
    }

    public async Task<SwapTransaction> CreatePendingSwapTransactionAsync(
        int userId,
        int vehicleId,
        int stationId,
        int batteryTakenId)
    {
        try
        {
            // Validate pin giao phải là Booked
            var batteryTaken = await _batteryRepo.GetByIdAsync(batteryTakenId);
            if (batteryTaken == null)
            {
                throw new ArgumentException($"Pin giao không tồn tại: {batteryTakenId}");
            }

            if (batteryTaken.Status != BatteryStatus.Booked)
            {
                throw new InvalidOperationException($"Chỉ được giao pin có trạng thái Booked. Pin {batteryTakenId} hiện có trạng thái {batteryTaken.Status}");
            }

            var transaction = new SwapTransaction
            {
                UserId = userId,
                VehicleId = vehicleId,
                StationId = stationId,
                BatteryTakenId = batteryTakenId,
                BatteryReturnedId = null,
                TotalCost = 0m,
                SwapTime = DateTime.UtcNow,
                Status = SwapStatus.Pending
            };

            await _swapTransactionRepo.CreateAsync(transaction);

            _logger.LogInformation("Pending swap transaction created: TransactionId={TransactionId}, UserId={UserId}, VehicleId={VehicleId}, StationId={StationId}, BatteryTaken={BatteryTakenId}",
                transaction.TransactionId, userId, vehicleId, stationId, batteryTakenId);

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create pending swap transaction for UserId: {UserId}, BatteryTaken: {BatteryTakenId}",
                userId, batteryTakenId);
            throw;
        }
    }

    public async Task<SwapTransaction> CompleteSwapTransactionAsync(
        int userId,
        int vehicleId,
        int stationId,
        int batteryTakenId,
        int batteryReturnedId,
        decimal totalCost,
        BatteryStatus returnedBatteryStatus = BatteryStatus.Charging)
    {
        try
        {
            // Lấy transaction pending
            var transaction = await _swapTransactionRepo.GetPendingTransactionAsync(userId, vehicleId, stationId, batteryTakenId);
            if (transaction == null)
            {
                throw new InvalidOperationException($"Không tìm thấy swap transaction pending cho UserId={userId}, VehicleId={vehicleId}, StationId={stationId}, BatteryTakenId={batteryTakenId}");
            }

            if (transaction.Status != SwapStatus.Pending)
            {
                throw new InvalidOperationException($"Swap transaction {transaction.TransactionId} không ở trạng thái Pending. Trạng thái hiện tại: {transaction.Status}");
            }

            // Validate pin nhận tồn tại
            var batteryReturned = await _batteryRepo.GetByIdAsync(batteryReturnedId);
            if (batteryReturned == null)
            {
                throw new ArgumentException($"Pin nhận không tồn tại: {batteryReturnedId}");
            }

            // Cập nhật transaction
            transaction.BatteryReturnedId = batteryReturnedId;
            transaction.TotalCost = totalCost;
            transaction.Status = SwapStatus.Completed;
            transaction.SwapTime = DateTime.UtcNow;

            await _swapTransactionRepo.UpdateAsync(transaction);

            // Cập nhật trạng thái pin
            // Pin cũ (nhận về) → charging hoặc defective (tùy chọn của staff)
            await _batteryRepo.UpdateBatteryStatusAsync(batteryReturnedId, returnedBatteryStatus);

            // Pin mới (giao) → taken
            await _batteryRepo.UpdateBatteryStatusAsync(transaction.BatteryTakenId, BatteryStatus.Taken);

            _logger.LogInformation("Swap transaction completed: TransactionId={TransactionId}, UserId={UserId}, BatteryReturned={BatteryReturnedId}",
                transaction.TransactionId, userId, batteryReturnedId);

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete swap transaction for UserId: {UserId}, BatteryReturned: {BatteryReturnedId}",
                userId, batteryReturnedId);
            throw;
        }
    }

    public async Task<bool> CancelSwapTransactionAsync(int userId, int vehicleId, int stationId, int batteryTakenId)
    {
        try
        {
            var transaction = await _swapTransactionRepo.GetPendingTransactionAsync(userId, vehicleId, stationId, batteryTakenId);
            if (transaction == null)
            {
                _logger.LogWarning("Swap transaction not found for UserId={UserId}, VehicleId={VehicleId}, StationId={StationId}, BatteryTakenId={BatteryTakenId}", 
                    userId, vehicleId, stationId, batteryTakenId);
                return false;
            }

            if (transaction.Status != SwapStatus.Pending)
            {
                _logger.LogWarning("Cannot cancel swap transaction {TransactionId} with status {Status}", 
                    transaction.TransactionId, transaction.Status);
                return false;
            }

            // Cập nhật status thành Cancelled
            transaction.Status = SwapStatus.Cancelled;
            await _swapTransactionRepo.UpdateAsync(transaction);

            // Reset pin status về Full
            await _batteryRepo.UpdateBatteryStatusAsync(transaction.BatteryTakenId, BatteryStatus.Full);

            _logger.LogInformation("Swap transaction cancelled: TransactionId={TransactionId}, UserId={UserId}",
                transaction.TransactionId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel swap transaction for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<SwapTransaction?> GetTransactionByIdAsync(int transactionId)
    {
        try
        {
            return await _swapTransactionRepo.GetTransactionWithDetailsAsync(transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get swap transaction {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<SwapTransaction?> GetPendingTransactionAsync(int userId, int vehicleId, int stationId, int batteryTakenId)
    {
        try
        {
            return await _swapTransactionRepo.GetPendingTransactionAsync(userId, vehicleId, stationId, batteryTakenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending swap transaction for UserId: {UserId}, VehicleId: {VehicleId}, StationId: {StationId}, BatteryTakenId: {BatteryTakenId}", 
                userId, vehicleId, stationId, batteryTakenId);
            throw;
        }
    }

    public async Task<IEnumerable<SwapTransaction>> GetUserTransactionsAsync(int userId)
    {
        try
        {
            return await _swapTransactionRepo.GetTransactionsByUserIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transactions for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<SwapTransaction>> GetStationTransactionsAsync(int stationId)
    {
        try
        {
            return await _swapTransactionRepo.GetTransactionsByStationIdAsync(stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transactions for StationId: {StationId}", stationId);
            throw;
        }
    }
}


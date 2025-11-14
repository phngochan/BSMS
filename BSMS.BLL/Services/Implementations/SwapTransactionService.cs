using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class SwapTransactionService : ISwapTransactionService
{

    private readonly ISwapTransactionRepository _swapTransactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChangingStationRepository _stationRepository;
    private readonly IBatteryRepository _batteryRepository;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<SwapTransactionService> _logger;

    public SwapTransactionService(
        ISwapTransactionRepository swapRepository,
        IUserRepository userRepository,
        IChangingStationRepository stationRepository,
        IBatteryRepository batteryRepository,
        IPaymentService paymentService,
        ILogger<SwapTransactionService> logger)
    {
        _swapTransactionRepository = swapRepository;
        _userRepository = userRepository;
        _stationRepository = stationRepository;
        _batteryRepository = batteryRepository;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<SwapTransaction> CreateTransactionAsync(SwapTransaction transaction)
    {
        var user = await _userRepository.GetSingleAsync(u => u.UserId == transaction.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var station = await _stationRepository.GetSingleAsync(s => s.StationId == transaction.StationId);
        if (station == null)
        {
            throw new InvalidOperationException("Station not found");
        }

        transaction.SwapTime = transaction.SwapTime == default ? DateTime.UtcNow : transaction.SwapTime;
        transaction.Status = SwapStatus.Pending;

        return await _swapTransactionRepository.CreateAsync(transaction);
    }

    public async Task DeleteTransactionAsync(int transactionId)
    {
        var existing = await _swapTransactionRepository.GetSingleAsync(t => t.TransactionId == transactionId);
        if (existing == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        await _swapTransactionRepository.DeleteAsync(existing);
    }

    public Task<int> GetDailyTransactionCountAsync(DateTime date) =>
        _swapTransactionRepository.CountDailyTransactionsAsync(date);

    public Task<decimal> GetCurrentMonthRevenueAsync() =>
        _swapTransactionRepository.GetRevenueForCurrentMonthAsync();

    public async Task<IEnumerable<SwapTransaction>> GetRecentTransactionsAsync(int count = 25)
    {
        return await _swapTransactionRepository.GetRecentTransactionsAsync(count);
    }

    public async Task<SwapTransaction?> GetTransactionAsync(int transactionId)
    {
        return await _swapTransactionRepository.GetTransactionWithDetailsAsync(transactionId);
    }

    public async Task<IEnumerable<SwapTransaction>> GetTransactionsByStationAsync(int stationId)
    {
        return await _swapTransactionRepository.GetTransactionsByStationAsync(stationId);
    }

    public async Task UpdateTransactionAsync(SwapTransaction transaction)
    {
        var existing = await _swapTransactionRepository.GetSingleAsync(t => t.TransactionId == transaction.TransactionId);
        if (existing == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        existing.VehicleId = transaction.VehicleId;
        existing.BatteryTakenId = transaction.BatteryTakenId;
        existing.BatteryReturnedId = transaction.BatteryReturnedId;
        existing.PaymentId = transaction.PaymentId;
        existing.TotalCost = transaction.TotalCost;
        existing.Status = transaction.Status;

        await _swapTransactionRepository.UpdateAsync(existing);
    }

    public async Task UpdateTransactionStatusAsync(int transactionId, SwapStatus status)
    {
        var existing = await _swapTransactionRepository.GetSingleAsync(t => t.TransactionId == transactionId);
        if (existing == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        existing.Status = status;
        await _swapTransactionRepository.UpdateAsync(existing);
    }

    public async Task<IDictionary<int, DateTime>> GetLatestCompletedSwapTimesAsync()
    {
        return await _swapTransactionRepository.GetLatestCompletedSwapTimesAsync();
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
            var batteryTaken = await _batteryRepository.GetByIdAsync(batteryTakenId);
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

            await _swapTransactionRepository.CreateAsync(transaction);
            var payment = await _paymentService.CreateCustomPaymentAsync(
                userId,
                transaction.TotalCost,
                PaymentMethod.Cash,
                PaymentStatus.Pending,
                $"SWAP:{transaction.TransactionId}");

            transaction.PaymentId = payment.PaymentId;
            await UpdateTransactionAsync(transaction);

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
            var transaction = await _swapTransactionRepository.GetPendingTransactionAsync(userId, vehicleId, stationId, batteryTakenId);
            if (transaction == null)
            {
                throw new InvalidOperationException($"Không tìm thấy swap transaction pending cho UserId={userId}, VehicleId={vehicleId}, StationId={stationId}, BatteryTakenId={batteryTakenId}");
            }

            if (transaction.Status != SwapStatus.Pending)
            {
                throw new InvalidOperationException($"Swap transaction {transaction.TransactionId} không ở trạng thái Pending. Trạng thái hiện tại: {transaction.Status}");
            }

            // Validate pin nhận tồn tại
            var batteryReturned = await _batteryRepository.GetByIdAsync(batteryReturnedId);
            if (batteryReturned == null)
            {
                throw new ArgumentException($"Pin nhận không tồn tại: {batteryReturnedId}");
            }

            // Cập nhật transaction
            transaction.BatteryReturnedId = batteryReturnedId;
            transaction.TotalCost = totalCost;
            transaction.Status = SwapStatus.Completed;
            transaction.SwapTime = DateTime.UtcNow;

            await _swapTransactionRepository.UpdateAsync(transaction);

            // Cập nhật trạng thái pin
            // Pin cũ (nhận về) → charging hoặc defective (tùy chọn của staff)
            await _batteryRepository.UpdateBatteryStatusAsync(batteryReturnedId, returnedBatteryStatus);

            // Pin mới (giao) → taken
            await _batteryRepository.UpdateBatteryStatusAsync(transaction.BatteryTakenId, BatteryStatus.Taken);

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
            var transaction = await _swapTransactionRepository.GetPendingTransactionAsync(userId, vehicleId, stationId, batteryTakenId);
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
            await _swapTransactionRepository.UpdateAsync(transaction);

            // Reset pin status về Full
            await _batteryRepository.UpdateBatteryStatusAsync(transaction.BatteryTakenId, BatteryStatus.Full);

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
            return await _swapTransactionRepository.GetTransactionWithDetailsAsync(transactionId);
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
            return await _swapTransactionRepository.GetPendingTransactionAsync(userId, vehicleId, stationId, batteryTakenId);
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
            return await _swapTransactionRepository.GetTransactionsByUserIdAsync(userId);
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
            return await _swapTransactionRepository.GetTransactionsByStationIdAsync(stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transactions for StationId: {StationId}", stationId);
            throw;
        }
    }
}


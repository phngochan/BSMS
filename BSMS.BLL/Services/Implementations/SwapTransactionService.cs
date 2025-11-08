using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class SwapTransactionService : ISwapTransactionService
{
    private readonly ISwapTransactionRepository _swapRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChangingStationRepository _stationRepository;
    private readonly IBatteryRepository _batteryRepository;

    public SwapTransactionService(
        ISwapTransactionRepository swapRepository,
        IUserRepository userRepository,
        IChangingStationRepository stationRepository,
        IBatteryRepository batteryRepository)
    {
        _swapRepository = swapRepository;
        _userRepository = userRepository;
        _stationRepository = stationRepository;
        _batteryRepository = batteryRepository;
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

        return await _swapRepository.CreateAsync(transaction);
    }

    public async Task DeleteTransactionAsync(int transactionId)
    {
        var existing = await _swapRepository.GetSingleAsync(t => t.TransactionId == transactionId);
        if (existing == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        await _swapRepository.DeleteAsync(existing);
    }

    public async Task<int> GetDailyTransactionCountAsync(DateTime date)
    {
        return await _swapRepository.CountDailyTransactionsAsync(date);
    }

    public async Task<decimal> GetCurrentMonthRevenueAsync()
    {
        return await _swapRepository.GetRevenueForCurrentMonthAsync();
    }

    public async Task<IEnumerable<SwapTransaction>> GetRecentTransactionsAsync(int count = 25)
    {
        return await _swapRepository.GetRecentTransactionsAsync(count);
    }

    public async Task<SwapTransaction?> GetTransactionAsync(int transactionId)
    {
        return await _swapRepository.GetTransactionWithDetailsAsync(transactionId);
    }

    public async Task<IEnumerable<SwapTransaction>> GetTransactionsByStationAsync(int stationId)
    {
        return await _swapRepository.GetTransactionsByStationAsync(stationId);
    }

    public async Task UpdateTransactionAsync(SwapTransaction transaction)
    {
        var existing = await _swapRepository.GetSingleAsync(t => t.TransactionId == transaction.TransactionId);
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

        await _swapRepository.UpdateAsync(existing);
    }

    public async Task UpdateTransactionStatusAsync(int transactionId, SwapStatus status)
    {
        var existing = await _swapRepository.GetSingleAsync(t => t.TransactionId == transactionId);
        if (existing == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        existing.Status = status;
        await _swapRepository.UpdateAsync(existing);
    }
}

using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class BatteryTransferService : IBatteryTransferService
{
    private readonly IBatteryTransferRepository _transferRepository;

    public BatteryTransferService(IBatteryTransferRepository transferRepository)
    {
        _transferRepository = transferRepository;
    }

    public async Task<IEnumerable<BatteryTransfer>> GetRecentTransfersAsync(int count = 20)
    {
        var transfers = await _transferRepository.GetAllAsync(
            null,
            q => q.OrderByDescending(t => t.TransferTime),
            t => t.Battery,
            t => t.FromStation,
            t => t.ToStation,
            t => t.ConfirmedByUser
        );
        return transfers.Take(count);
    }

    public async Task<IEnumerable<BatteryTransfer>> GetTransfersByStationAsync(int stationId)
    {
        return await _transferRepository.GetAllAsync(
            t => t.FromStationId == stationId || t.ToStationId == stationId,
            q => q.OrderByDescending(t => t.TransferTime),
            t => t.Battery,
            t => t.FromStation,
            t => t.ToStation,
            t => t.ConfirmedByUser
        );
    }

    public async Task<IEnumerable<BatteryTransfer>> GetTransfersInProgressAsync()
    {
        return await _transferRepository.GetAllAsync(
            t => t.Status == TransferStatus.InProgress,
            q => q.OrderBy(t => t.TransferTime),
            t => t.Battery,
            t => t.FromStation,
            t => t.ToStation
        );
    }

    public async Task<IEnumerable<BatteryTransfer>> GetIncomingTransfersAsync(int toStationId)
    {
        return await _transferRepository.GetAllAsync(
            t => t.ToStationId == toStationId && t.Status == TransferStatus.InProgress,
            q => q.OrderBy(t => t.TransferTime),
            t => t.Battery,
            t => t.FromStation,
            t => t.ToStation
        );
    }

    // ✅ LỊCH SỬ ĐIỀU PHỐI
    public async Task<IEnumerable<BatteryTransfer>> GetTransferHistoryAsync(int stationId, int pageNumber = 1, int pageSize = 20)
    {
        var result = await _transferRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            t => (t.FromStationId == stationId || t.ToStationId == stationId) &&
                 (t.Status == TransferStatus.Completed ||
                  t.Status == TransferStatus.Rejected ||
                  t.Status == TransferStatus.Cancelled),
            q => q.OrderByDescending(t => t.CompletedAt ?? t.TransferTime),
            t => t.Battery,
            t => t.FromStation,
            t => t.ToStation,
            t => t.ConfirmedByUser
        );
        return result.Items;
    }

    public async Task<BatteryTransfer?> GetTransferAsync(int transferId)
    {
        return await _transferRepository.GetByIdAsync(
            transferId,
            t => t.Battery,
            t => t.FromStation,
            t => t.ToStation,
            t => t.ConfirmedByUser
        );
    }

    public async Task<BatteryTransfer> CreateTransferAsync(BatteryTransfer transfer)
    {
        return await _transferRepository.CreateAsync(transfer);
    }

    public async Task UpdateTransferAsync(BatteryTransfer transfer)
    {
        await _transferRepository.UpdateAsync(transfer);
    }

    public async Task UpdateTransferStatusAsync(int transferId, TransferStatus status)
    {
        var transfer = await _transferRepository.GetByIdAsync(transferId);
        if (transfer != null)
        {
            transfer.Status = status;
            if (status == TransferStatus.Completed || 
                status == TransferStatus.Rejected || 
                status == TransferStatus.Cancelled)
            {
                transfer.CompletedAt = DateTime.UtcNow;
            }
            await _transferRepository.UpdateAsync(transfer);
        }
    }

    // ✅ XÁC NHẬN NHẬN PIN
    public async Task ConfirmTransferAsync(int transferId, int confirmedByUserId)
    {
        var transfer = await _transferRepository.GetByIdAsync(transferId);
        if (transfer != null)
        {
            transfer.Status = TransferStatus.Completed;
            transfer.CompletedAt = DateTime.UtcNow;
            transfer.ConfirmedByUserId = confirmedByUserId;
            await _transferRepository.UpdateAsync(transfer);
        }
    }

    // ✅ TỪ CHỐI NHẬN PIN
    public async Task RejectTransferAsync(int transferId, int rejectedByUserId, string reason)
    {
        var transfer = await _transferRepository.GetByIdAsync(transferId);
        if (transfer != null)
        {
            transfer.Status = TransferStatus.Rejected;
            transfer.CompletedAt = DateTime.UtcNow;
            transfer.ConfirmedByUserId = rejectedByUserId;
            transfer.RejectionReason = reason;
            await _transferRepository.UpdateAsync(transfer);
        }
    }

    public async Task DeleteTransferAsync(int transferId)
    {
        await _transferRepository.DeleteAsync(transferId);
    }
}

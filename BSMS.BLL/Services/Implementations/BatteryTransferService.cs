using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class BatteryTransferService : IBatteryTransferService
{
    private readonly IBatteryTransferRepository _transferRepository;
    private readonly IBatteryRepository _batteryRepository;

    public BatteryTransferService(
        IBatteryTransferRepository transferRepository,
        IBatteryRepository batteryRepository)
    {
        _transferRepository = transferRepository;
        _batteryRepository = batteryRepository;
    }

    public async Task<BatteryTransfer> CreateTransferAsync(BatteryTransfer transfer)
    {
        var battery = await _batteryRepository.GetSingleAsync(b => b.BatteryId == transfer.BatteryId);
        if (battery == null)
        {
            throw new InvalidOperationException("Battery not found");
        }

        if (battery.Status != BatteryStatus.Full)
        {
            throw new InvalidOperationException("Chỉ có thể điều phối pin đang ở trạng thái Full.");
        }

        if (battery.StationId != transfer.FromStationId)
        {
            throw new InvalidOperationException("Battery is not available at the source station");
        }

        transfer.TransferTime = transfer.TransferTime == default
            ? DateTime.UtcNow
            : transfer.TransferTime;
        transfer.Status = TransferStatus.InProgress;

        return await _transferRepository.CreateAsync(transfer);
    }

    public async Task DeleteTransferAsync(int transferId)
    {
        var existing = await _transferRepository.GetSingleAsync(t => t.TransferId == transferId);
        if (existing == null)
        {
            throw new InvalidOperationException("Transfer not found");
        }

        await _transferRepository.DeleteAsync(existing);
    }

    public async Task<IEnumerable<BatteryTransfer>> GetRecentTransfersAsync(int count = 20)
    {
        return await _transferRepository.GetRecentTransfersAsync(count);
    }

    public async Task<BatteryTransfer?> GetTransferAsync(int transferId)
    {
        return await _transferRepository.GetTransferWithDetailsAsync(transferId);
    }

    public async Task<IEnumerable<BatteryTransfer>> GetTransfersByStationAsync(int stationId)
    {
        return await _transferRepository.GetTransfersByStationAsync(stationId);
    }

    public async Task<IEnumerable<BatteryTransfer>> GetTransfersInProgressAsync()
    {
        return await _transferRepository.GetTransfersInProgressAsync();
    }

    public async Task UpdateTransferAsync(BatteryTransfer transfer)
    {
        var existing = await _transferRepository.GetSingleAsync(t => t.TransferId == transfer.TransferId);
        if (existing == null)
        {
            throw new InvalidOperationException("Transfer not found");
        }

        var battery = await _batteryRepository.GetSingleAsync(b => b.BatteryId == transfer.BatteryId);
        if (battery == null)
        {
            throw new InvalidOperationException("Battery not found");
        }

        if (battery.Status != BatteryStatus.Full)
        {
            throw new InvalidOperationException("Chỉ có thể điều phối pin đang ở trạng thái Full.");
        }

        if (battery.StationId != transfer.FromStationId)
        {
            throw new InvalidOperationException("Battery is not available at the source station");
        }

        existing.BatteryId = transfer.BatteryId;
        existing.FromStationId = transfer.FromStationId;
        existing.ToStationId = transfer.ToStationId;
        existing.TransferTime = transfer.TransferTime;
        existing.Status = transfer.Status;

        await _transferRepository.UpdateAsync(existing);
    }

    public async Task UpdateTransferStatusAsync(int transferId, TransferStatus status)
    {
        var existing = await _transferRepository.GetTransferWithDetailsAsync(transferId);
        if (existing == null)
        {
            throw new InvalidOperationException("Transfer not found");
        }

        existing.Status = status;
        await _transferRepository.UpdateAsync(existing);

        if (status == TransferStatus.Completed)
        {
            var battery = await _batteryRepository.GetSingleAsync(b => b.BatteryId == existing.BatteryId);
            if (battery != null)
            {
                battery.StationId = existing.ToStationId;
                battery.Status = BatteryStatus.Full;
                battery.UpdatedAt = DateTime.UtcNow;
                await _batteryRepository.UpdateAsync(battery);
            }
        }
    }
}

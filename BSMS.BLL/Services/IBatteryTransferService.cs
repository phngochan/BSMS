using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IBatteryTransferService
{
    Task<IEnumerable<BatteryTransfer>> GetRecentTransfersAsync(int count = 20);
    Task<IEnumerable<BatteryTransfer>> GetTransfersByStationAsync(int stationId);
    Task<IEnumerable<BatteryTransfer>> GetTransfersInProgressAsync();
    Task<BatteryTransfer?> GetTransferAsync(int transferId);
    Task<BatteryTransfer> CreateTransferAsync(BatteryTransfer transfer);
    Task UpdateTransferAsync(BatteryTransfer transfer);
    Task UpdateTransferStatusAsync(int transferId, TransferStatus status);
    Task DeleteTransferAsync(int transferId);
}

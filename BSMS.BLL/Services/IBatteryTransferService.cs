using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IBatteryTransferService
{
    Task<IEnumerable<BatteryTransfer>> GetRecentTransfersAsync(int count = 20);
    Task<IEnumerable<BatteryTransfer>> GetTransfersByStationAsync(int stationId);
    Task<IEnumerable<BatteryTransfer>> GetTransfersInProgressAsync();
    Task<IEnumerable<BatteryTransfer>> GetIncomingTransfersAsync(int toStationId);
    Task<IEnumerable<BatteryTransfer>> GetTransferHistoryAsync(int stationId, int pageNumber = 1, int pageSize = 20); // ✅ LỊCH SỬ
    Task<BatteryTransfer?> GetTransferAsync(int transferId);
    Task<BatteryTransfer> CreateTransferAsync(BatteryTransfer transfer);
    Task UpdateTransferAsync(BatteryTransfer transfer);
    Task UpdateTransferStatusAsync(int transferId, TransferStatus status);
    Task ConfirmTransferAsync(int transferId, int confirmedByUserId); // ✅ XÁC NHẬN
    Task RejectTransferAsync(int transferId, int rejectedByUserId, string reason); // ✅ TỪ CHỐI
    Task DeleteTransferAsync(int transferId);
}

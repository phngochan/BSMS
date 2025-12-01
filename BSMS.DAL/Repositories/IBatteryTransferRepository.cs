using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IBatteryTransferRepository : IGenericRepository<BatteryTransfer>
{
    Task<IEnumerable<BatteryTransfer>> GetRecentTransfersAsync(int count = 20);
    Task<IEnumerable<BatteryTransfer>> GetTransfersByStationAsync(int stationId);
    Task<IEnumerable<BatteryTransfer>> GetTransfersInProgressAsync();
    Task<BatteryTransfer?> GetTransferWithDetailsAsync(int transferId);
}

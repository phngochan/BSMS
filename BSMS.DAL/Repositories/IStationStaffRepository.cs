using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IStationStaffRepository : IGenericRepository<StationStaff>
{
    Task<IEnumerable<StationStaff>> GetAssignmentsAsync();
    Task<IEnumerable<StationStaff>> GetAssignmentsByStationAsync(int stationId);
    Task<StationStaff?> GetAssignmentWithDetailsAsync(int staffId);
    Task<bool> IsUserAssignedAsync(int userId, int stationId);
}

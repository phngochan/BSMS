using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IStationStaffRepository : IGenericRepository<StationStaff>
{
    Task<IEnumerable<StationStaff>> GetAssignmentsAsync();
    Task<IEnumerable<StationStaff>> GetAssignmentsByStationAsync(int stationId);
    Task<StationStaff?> GetAssignmentByUserAsync(int userId);
    Task<StationStaff?> GetAssignmentWithDetailsAsync(int staffId);
    Task<IEnumerable<StationStaff>> GetActiveStaffByStationAsync(int stationId, TimeSpan currentTime);
    
    // ✅ NEW methods
    Task<IEnumerable<StationStaff>> GetActiveAssignmentsByUserAsync(int userId);
    Task<IEnumerable<StationStaff>> GetOverlappingShiftsAsync(
        int stationId, 
        TimeSpan shiftStart, 
        TimeSpan shiftEnd, 
        int? excludeStaffId = null);
    Task<int> CountActiveStaffInShiftAsync(int stationId, TimeSpan shiftStart, TimeSpan shiftEnd);
}

using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IStationStaffService
{
    Task<StationStaff> AssignStaffAsync(StationStaff assignment);
    Task<IEnumerable<StationStaff>> GetAssignmentsAsync();
    Task<IEnumerable<StationStaff>> GetAssignmentsByStationAsync(int stationId);
    Task<StationStaff?> GetAssignmentForUserAsync(int userId);
    Task<StationStaff?> GetAssignmentAsync(int staffId);
    Task RemoveAssignmentAsync(int staffId);
    Task UpdateAssignmentAsync(StationStaff assignment);
    
    // ✅ Working status methods
    Task<bool> IsStaffCurrentlyWorkingAsync(int userId);
    Task<StationStaff?> GetCurrentActiveShiftAsync(int userId);
    Task<IEnumerable<StationStaff>> GetStaffCurrentlyWorkingAtStationAsync(int stationId);
    
    // ✅ Validation methods
    Task ValidateShiftTimingAsync(TimeSpan shiftStart, TimeSpan shiftEnd);
    Task<bool> HasScheduleConflictAsync(int userId, TimeSpan shiftStart, TimeSpan shiftEnd, int? excludeStaffId = null);
}

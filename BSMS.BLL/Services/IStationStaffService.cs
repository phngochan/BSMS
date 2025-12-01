using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IStationStaffService
{
    Task<IEnumerable<StationStaff>> GetAssignmentsAsync();
    Task<IEnumerable<StationStaff>> GetAssignmentsByStationAsync(int stationId);
    Task<StationStaff?> GetAssignmentAsync(int staffId);
    Task<StationStaff?> GetAssignmentForUserAsync(int userId);
    Task<StationStaff> AssignStaffAsync(StationStaff assignment);
    Task UpdateAssignmentAsync(StationStaff assignment);
    Task RemoveAssignmentAsync(int staffId);
}

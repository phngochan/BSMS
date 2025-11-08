using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class StationStaffService : IStationStaffService
{
    private readonly IStationStaffRepository _stationStaffRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChangingStationRepository _stationRepository;

    public StationStaffService(
        IStationStaffRepository stationStaffRepository,
        IUserRepository userRepository,
        IChangingStationRepository stationRepository)
    {
        _stationStaffRepository = stationStaffRepository;
        _userRepository = userRepository;
        _stationRepository = stationRepository;
    }

    public async Task<StationStaff> AssignStaffAsync(StationStaff assignment)
    {
        var user = await _userRepository.GetSingleAsync(u => u.UserId == assignment.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var station = await _stationRepository.GetSingleAsync(s => s.StationId == assignment.StationId);
        if (station == null)
        {
            throw new InvalidOperationException("Station not found");
        }

        var isAssigned = await _stationStaffRepository.IsUserAssignedAsync(assignment.UserId, assignment.StationId);
        if (isAssigned)
        {
            throw new InvalidOperationException("User already assigned to this station");
        }

        assignment.AssignedAt = assignment.AssignedAt == default ? DateTime.UtcNow : assignment.AssignedAt;

        return await _stationStaffRepository.CreateAsync(assignment);
    }

    public async Task<IEnumerable<StationStaff>> GetAssignmentsAsync()
    {
        return await _stationStaffRepository.GetAssignmentsAsync();
    }

    public async Task<IEnumerable<StationStaff>> GetAssignmentsByStationAsync(int stationId)
    {
        return await _stationStaffRepository.GetAssignmentsByStationAsync(stationId);
    }

    public async Task<StationStaff?> GetAssignmentAsync(int staffId)
    {
        return await _stationStaffRepository.GetAssignmentWithDetailsAsync(staffId);
    }

    public async Task RemoveAssignmentAsync(int staffId)
    {
        var existing = await _stationStaffRepository.GetSingleAsync(ss => ss.StaffId == staffId);
        if (existing == null)
        {
            throw new InvalidOperationException("Assignment not found");
        }

        await _stationStaffRepository.DeleteAsync(existing);
    }

    public async Task UpdateAssignmentAsync(StationStaff assignment)
    {
        var existing = await _stationStaffRepository.GetSingleAsync(ss => ss.StaffId == assignment.StaffId);
        if (existing == null)
        {
            throw new InvalidOperationException("Assignment not found");
        }

        if (existing.UserId != assignment.UserId)
        {
            var user = await _userRepository.GetSingleAsync(u => u.UserId == assignment.UserId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            var isAssigned = await _stationStaffRepository.IsUserAssignedAsync(assignment.UserId, existing.StationId);
            if (isAssigned)
            {
                throw new InvalidOperationException("User already assigned to this station");
            }
        }

        existing.UserId = assignment.UserId;
        if (existing.StationId != assignment.StationId)
        {
            var station = await _stationRepository.GetSingleAsync(s => s.StationId == assignment.StationId);
            if (station == null)
            {
                throw new InvalidOperationException("Station not found");
            }
        }

        existing.StationId = assignment.StationId;
        existing.AssignedAt = assignment.AssignedAt;

        await _stationStaffRepository.UpdateAsync(existing);
    }
}

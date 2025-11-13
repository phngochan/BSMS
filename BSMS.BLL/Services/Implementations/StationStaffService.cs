using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class StationStaffService : IStationStaffService
{
    private readonly IStationStaffRepository _stationStaffRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChangingStationRepository _stationRepository;
    private readonly ILogger<StationStaffService>? _logger;
    
    // ✅ Business rules constants
    private const int MIN_SHIFT_HOURS = 4;
    private const int MAX_SHIFT_HOURS = 12;
    private const int SHIFT_START_HOUR = 6;
    private const int SHIFT_END_HOUR = 22;
    private const int MAX_STAFF_PER_SHIFT = 3; // ✅ Giới hạn nhân sự mỗi ca

    public StationStaffService(
        IStationStaffRepository stationStaffRepository,
        IUserRepository userRepository,
        IChangingStationRepository stationRepository,
        ILogger<StationStaffService>? logger = null)
    {
        _stationStaffRepository = stationStaffRepository;
        _userRepository = userRepository;
        _stationRepository = stationRepository;
        _logger = logger;
    }

    public async Task<StationStaff> AssignStaffAsync(StationStaff assignment)
    {
        // ✅ 1. Validate user exists and has correct role
        var user = await _userRepository.GetSingleAsync(u => u.UserId == assignment.UserId);
        if (user == null)
        {
            _logger?.LogWarning("[StaffSchedule] User not found: {UserId}", assignment.UserId);
            throw new InvalidOperationException("Không tìm thấy người dùng.");
        }
        if (user.Role != UserRole.StationStaff)
        {
            _logger?.LogWarning("[StaffSchedule] User {UserId} is not StationStaff role", assignment.UserId);
            throw new InvalidOperationException("Chỉ có thể gán người dùng với vai trò StationStaff.");
        }

        // ✅ 2. Validate station exists
        var station = await _stationRepository.GetSingleAsync(s => s.StationId == assignment.StationId);
        if (station == null)
        {
            _logger?.LogWarning("[StaffSchedule] Station not found: {StationId}", assignment.StationId);
            throw new InvalidOperationException("Không tìm thấy trạm.");
        }

        // ✅ 3. Validate shift timing   
        await ValidateShiftTimingAsync(assignment.ShiftStart, assignment.ShiftEnd);

        // ✅ 4. Check for existing active assignments for this user
        var existingActiveAssignments = await _stationStaffRepository
            .GetActiveAssignmentsByUserAsync(assignment.UserId);
        
        if (existingActiveAssignments.Any())
        {
            var conflictingAssignment = existingActiveAssignments.First();
            _logger?.LogWarning(
                "[StaffSchedule] User {UserId} already has active assignment at Station {StationId}",
                assignment.UserId, conflictingAssignment.StationId);
            
            throw new InvalidOperationException(
                $"Nhân viên {user.FullName} đã có ca làm việc active tại trạm {conflictingAssignment.Station?.Name}. " +
                $"Vui lòng deactivate ca cũ trước khi gán ca mới.");
        }

        // ✅ 5. Check for overlapping shifts at the same station
        var overlappingShifts = await _stationStaffRepository.GetOverlappingShiftsAsync(
            assignment.StationId,
            assignment.ShiftStart,
            assignment.ShiftEnd);
        
        if (overlappingShifts.Any())
        {
            var conflictStaff = overlappingShifts.First();
            _logger?.LogWarning(
                "[StaffSchedule] Shift overlap detected at Station {StationId} with Staff {StaffId}",
                assignment.StationId, conflictStaff.StaffId);
            
            throw new InvalidOperationException(
                $"Ca làm việc bị trùng với nhân viên {conflictStaff.User?.FullName} " +
                $"({conflictStaff.ShiftStart:hh\\:mm} - {conflictStaff.ShiftEnd:hh\\:mm})");
        }

        // ✅ 6. Check max staff limit per shift at station
        var currentStaffCount = await _stationStaffRepository.CountActiveStaffInShiftAsync(
            assignment.StationId,
            assignment.ShiftStart,
            assignment.ShiftEnd);
        
        if (currentStaffCount >= MAX_STAFF_PER_SHIFT)
        {
            _logger?.LogWarning(
                "[StaffSchedule] Max staff limit reached at Station {StationId} for shift {ShiftStart}-{ShiftEnd}",
                assignment.StationId, assignment.ShiftStart, assignment.ShiftEnd);
            
            throw new InvalidOperationException(
                $"Trạm {station.Name} đã đủ {MAX_STAFF_PER_SHIFT} nhân viên cho ca này. " +
                $"Không thể thêm nhân viên mới.");
        }

        // ✅ 7. Set assignment time (auto if not provided)
        if (assignment.AssignedAt == default || assignment.AssignedAt == DateTime.MinValue)
        {
            assignment.AssignedAt = DateTime.UtcNow;
        }
        
        assignment.IsActive = true;

        // ✅ 8. Create assignment
        var createdAssignment = await _stationStaffRepository.CreateAsync(assignment);
        
        _logger?.LogInformation(
            "[StaffSchedule] CREATED Assignment - StaffId: {StaffId}, User: {UserName} ({UserId}), " +
            "Station: {StationName} ({StationId}), Shift: {ShiftStart:hh\\:mm}-{ShiftEnd:hh\\:mm}, " +
            "AssignedAt: {AssignedAt:yyyy-MM-dd HH:mm:ss}",
            createdAssignment.StaffId,
            user.FullName,
            assignment.UserId,
            station.Name,
            assignment.StationId,
            assignment.ShiftStart,
            assignment.ShiftEnd,
            assignment.AssignedAt);
        
        return createdAssignment;
    }

    public async Task UpdateAssignmentAsync(StationStaff assignment)
    {
        var existing = await _stationStaffRepository.GetSingleAsync(ss => ss.StaffId == assignment.StaffId);
        if (existing == null)
        {
            _logger?.LogWarning("[StaffSchedule] Assignment not found: {StaffId}", assignment.StaffId);
            throw new InvalidOperationException("Không tìm thấy phân công.");
        }

        // ✅ Validate shift timing
        await ValidateShiftTimingAsync(assignment.ShiftStart, assignment.ShiftEnd);

        // ✅ Validate user change
        if (existing.UserId != assignment.UserId)
        {
            var user = await _userRepository.GetSingleAsync(u => u.UserId == assignment.UserId);
            if (user == null)
            {
                throw new InvalidOperationException("Không tìm thấy người dùng.");
            }
            if (user.Role != UserRole.StationStaff)
            {
                throw new InvalidOperationException("Chỉ có thể gán người dùng với vai trò StationStaff.");
            }

            // Check for conflicts with new user
            var hasConflict = await HasScheduleConflictAsync(
                assignment.UserId, 
                assignment.ShiftStart, 
                assignment.ShiftEnd, 
                assignment.StaffId);
            
            if (hasConflict)
            {
                throw new InvalidOperationException("Nhân viên này đã có ca làm việc trùng thời gian.");
            }
        }

        // ✅ Check for overlapping shifts if time changed
        if (existing.ShiftStart != assignment.ShiftStart || existing.ShiftEnd != assignment.ShiftEnd)
        {
            var overlappingShifts = await _stationStaffRepository.GetOverlappingShiftsAsync(
                assignment.StationId,
                assignment.ShiftStart,
                assignment.ShiftEnd,
                assignment.StaffId);
            
            if (overlappingShifts.Any())
            {
                var conflictStaff = overlappingShifts.First();
                throw new InvalidOperationException(
                    $"Ca làm việc bị trùng với nhân viên {conflictStaff.User?.FullName} " +
                    $"({conflictStaff.ShiftStart:hh\\:mm} - {conflictStaff.ShiftEnd:hh\\:mm})");
            }
        }

        // Update fields
        var oldShiftStart = existing.ShiftStart;
        var oldShiftEnd = existing.ShiftEnd;
        var oldIsActive = existing.IsActive;
        
        existing.UserId = assignment.UserId;
        existing.StationId = assignment.StationId;
        existing.ShiftStart = assignment.ShiftStart;
        existing.ShiftEnd = assignment.ShiftEnd;
        existing.IsActive = assignment.IsActive;
        existing.AssignedAt = assignment.AssignedAt == default ? existing.AssignedAt : assignment.AssignedAt;

        await _stationStaffRepository.UpdateAsync(existing);
        
        _logger?.LogInformation(
            "[StaffSchedule] UPDATED Assignment - StaffId: {StaffId}, " +
            "Shift: {OldShift} → {NewShift}, Active: {OldActive} → {NewActive}, " +
            "UpdatedAt: {UpdatedAt:yyyy-MM-dd HH:mm:ss}",
            assignment.StaffId,
            $"{oldShiftStart:hh\\:mm}-{oldShiftEnd:hh\\:mm}",
            $"{assignment.ShiftStart:hh\\:mm}-{assignment.ShiftEnd:hh\\:mm}",
            oldIsActive,
            assignment.IsActive,
            DateTime.UtcNow);
    }

    public async Task RemoveAssignmentAsync(int staffId)
    {
        var existing = await _stationStaffRepository.GetSingleAsync(ss => ss.StaffId == staffId);
        if (existing == null)
        {
            throw new InvalidOperationException("Không tìm thấy phân công.");
        }

        await _stationStaffRepository.DeleteAsync(existing);
        
        _logger?.LogInformation(
            "[StaffSchedule] DELETED Assignment - StaffId: {StaffId}, " +
            "User: {UserId}, Station: {StationId}, Shift: {ShiftStart:hh\\:mm}-{ShiftEnd:hh\\:mm}",
            staffId,
            existing.UserId,
            existing.StationId,
            existing.ShiftStart,
            existing.ShiftEnd);
    }

    // ✅ Validation methods
    public Task ValidateShiftTimingAsync(TimeSpan shiftStart, TimeSpan shiftEnd)
    {
        if (shiftStart >= shiftEnd)
        {
            throw new InvalidOperationException("Thời gian bắt đầu ca phải trước thời gian kết thúc.");
        }

        var duration = shiftEnd - shiftStart;
        if (duration < TimeSpan.FromHours(MIN_SHIFT_HOURS))
        {
            throw new InvalidOperationException($"Ca làm việc phải ít nhất {MIN_SHIFT_HOURS} giờ.");
        }

        if (duration > TimeSpan.FromHours(MAX_SHIFT_HOURS))
        {
            throw new InvalidOperationException($"Ca làm việc không được vượt quá {MAX_SHIFT_HOURS} giờ.");
        }

        if (shiftStart < TimeSpan.FromHours(SHIFT_START_HOUR))
        {
            throw new InvalidOperationException($"Ca làm việc không được bắt đầu trước {SHIFT_START_HOUR}:00 sáng.");
        }

        if (shiftEnd > TimeSpan.FromHours(SHIFT_END_HOUR))
        {
            throw new InvalidOperationException($"Ca làm việc không được kết thúc sau {SHIFT_END_HOUR}:00 tối.");
        }

        return Task.CompletedTask;
    }

    public async Task<bool> HasScheduleConflictAsync(
        int userId, TimeSpan shiftStart, TimeSpan shiftEnd, int? excludeStaffId = null)
    {
        var existingAssignments = await _stationStaffRepository.GetActiveAssignmentsByUserAsync(userId);
        
        foreach (var assignment in existingAssignments)
        {
            if (excludeStaffId.HasValue && assignment.StaffId == excludeStaffId.Value)
            {
                continue;
            }

            bool hasOverlap = !(shiftEnd <= assignment.ShiftStart || shiftStart >= assignment.ShiftEnd);
            if (hasOverlap)
            {
                return true;
            }
        }

        return false;
    }

    // ✅ Other methods (existing)
    public async Task<IEnumerable<StationStaff>> GetAssignmentsAsync()
    {
        return await _stationStaffRepository.GetAssignmentsAsync();
    }

    public async Task<IEnumerable<StationStaff>> GetAssignmentsByStationAsync(int stationId)
    {
        return await _stationStaffRepository.GetAssignmentsByStationAsync(stationId);
    }

    public async Task<StationStaff?> GetAssignmentForUserAsync(int userId)
    {
        return await _stationStaffRepository.GetAssignmentByUserAsync(userId);
    }

    public async Task<StationStaff?> GetAssignmentAsync(int staffId)
    {
        return await _stationStaffRepository.GetAssignmentWithDetailsAsync(staffId);
    }

    public async Task<bool> IsStaffCurrentlyWorkingAsync(int userId)
    {
        var shift = await GetCurrentActiveShiftAsync(userId);
        return shift != null;
    }

    public async Task<StationStaff?> GetCurrentActiveShiftAsync(int userId)
    {
        var assignment = await _stationStaffRepository.GetAssignmentByUserAsync(userId);
        
        if (assignment == null || !assignment.IsActive)
        {
            return null;
        }

        var currentTime = DateTime.Now.TimeOfDay;
        
        if (currentTime >= assignment.ShiftStart && currentTime <= assignment.ShiftEnd)
        {
            return assignment;
        }

        return null;
    }

    public async Task<IEnumerable<StationStaff>> GetStaffCurrentlyWorkingAtStationAsync(int stationId)
    {
        var currentTime = DateTime.Now.TimeOfDay;
        return await _stationStaffRepository.GetActiveStaffByStationAsync(stationId, currentTime);
    }
}

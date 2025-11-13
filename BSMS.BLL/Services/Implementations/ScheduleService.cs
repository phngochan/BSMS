using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class ScheduleService : IScheduleService
{
    private readonly IStationStaffRepository _stationStaffRepository;
    private readonly IChangingStationRepository _stationRepository;
    
    // Business rules constants
    private const int MIN_SHIFT_HOURS = 4;
    private const int MAX_SHIFT_HOURS = 12;
    private const int MIN_STAFF_PER_STATION = 1;
    private const int SHIFT_START_HOUR = 6; // 6 AM
    private const int SHIFT_END_HOUR = 22; // 10 PM

    public ScheduleService(
        IStationStaffRepository stationStaffRepository,
        IChangingStationRepository stationRepository)
    {
        _stationStaffRepository = stationStaffRepository;
        _stationRepository = stationRepository;
    }

    public async Task<IEnumerable<StationStaff>> GetScheduleByStationAsync(
        int stationId, DateTime startDate, DateTime endDate)
    {
        var allAssignments = await _stationStaffRepository
            .GetAssignmentsByStationAsync(stationId);
        
        // Filter active assignments in date range
        return allAssignments
            .Where(s => s.IsActive && s.AssignedAt <= endDate)
            .OrderBy(s => s.ShiftStart)
            .ToList();
    }

    public async Task<IEnumerable<StationStaff>> GetScheduleByStaffAsync(
        int userId, DateTime startDate, DateTime endDate)
    {
        var assignment = await _stationStaffRepository
            .GetAssignmentByUserAsync(userId);
        
        if (assignment == null || !assignment.IsActive)
        {
            return Enumerable.Empty<StationStaff>();
        }

        return new List<StationStaff> { assignment };
    }

    public async Task<bool> HasScheduleConflictAsync(
        int userId, TimeSpan shiftStart, TimeSpan shiftEnd, int? excludeStaffId = null)
    {
        var existingAssignment = await _stationStaffRepository
            .GetAssignmentByUserAsync(userId);
        
        if (existingAssignment == null)
        {
            return false;
        }

        // Skip if checking the same assignment
        if (excludeStaffId.HasValue && existingAssignment.StaffId == excludeStaffId.Value)
        {
            return false;
        }

        if (!existingAssignment.IsActive)
        {
            return false;
        }

        // Check overlap: shifts overlap if NOT (end1 <= start2 OR start1 >= end2)
        bool hasOverlap = !(shiftEnd <= existingAssignment.ShiftStart || 
                           shiftStart >= existingAssignment.ShiftEnd);
        
        return hasOverlap;
    }

    public async Task<Dictionary<int, int>> GetStationCoverageReportAsync(DateTime date)
    {
        var allStations = await _stationRepository.GetAllAsync();
        var result = new Dictionary<int, int>();
        
        var currentTime = date.TimeOfDay;

        foreach (var station in allStations)
        {
            var workingStaff = await GetStaffCurrentlyWorkingAtStationAsync(station.StationId);
            result[station.StationId] = workingStaff.Count();
        }

        return result;
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
        
        // Check if current time is within shift
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

    public Task ValidateShiftTimingAsync(TimeSpan shiftStart, TimeSpan shiftEnd)
    {
        // Validate: start < end
        if (shiftStart >= shiftEnd)
        {
            throw new InvalidOperationException(
                "Thời gian bắt đầu ca phải trước thời gian kết thúc.");
        }

        // Validate: reasonable duration (4-12 hours)
        var duration = shiftEnd - shiftStart;
        if (duration < TimeSpan.FromHours(MIN_SHIFT_HOURS))
        {
            throw new InvalidOperationException(
                $"Ca làm việc phải ít nhất {MIN_SHIFT_HOURS} giờ.");
        }

        if (duration > TimeSpan.FromHours(MAX_SHIFT_HOURS))
        {
            throw new InvalidOperationException(
                $"Ca làm việc không được vượt quá {MAX_SHIFT_HOURS} giờ.");
        }

        // Validate: working hours compliance (6 AM - 10 PM)
        if (shiftStart < TimeSpan.FromHours(SHIFT_START_HOUR))
        {
            throw new InvalidOperationException(
                $"Ca làm việc không được bắt đầu trước {SHIFT_START_HOUR}:00 sáng.");
        }

        if (shiftEnd > TimeSpan.FromHours(SHIFT_END_HOUR))
        {
            throw new InvalidOperationException(
                $"Ca làm việc không được kết thúc sau {SHIFT_END_HOUR}:00 tối.");
        }

        return Task.CompletedTask;
    }

    public async Task<bool> HasAdequateCoverageAsync(int stationId)
    {
        var workingStaff = await GetStaffCurrentlyWorkingAtStationAsync(stationId);
        return workingStaff.Count() >= MIN_STAFF_PER_STATION;
    }
}

using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IScheduleService
{
    /// <summary>
    /// Lấy lịch làm việc của một station trong khoảng thời gian
    /// </summary>
    Task<IEnumerable<StationStaff>> GetScheduleByStationAsync(
        int stationId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Lấy lịch làm việc của một staff trong khoảng thời gian
    /// </summary>
    Task<IEnumerable<StationStaff>> GetScheduleByStaffAsync(
        int userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Kiểm tra xem có xung đột lịch không
    /// </summary>
    Task<bool> HasScheduleConflictAsync(
        int userId, TimeSpan shiftStart, TimeSpan shiftEnd, int? excludeStaffId = null);

    /// <summary>
    /// Lấy báo cáo độ phủ nhân sự của các station
    /// </summary>
    Task<Dictionary<int, int>> GetStationCoverageReportAsync(DateTime date);

    /// <summary>
    /// Kiểm tra xem staff có đang làm việc không
    /// </summary>
    Task<bool> IsStaffCurrentlyWorkingAsync(int userId);

    /// <summary>
    /// Lấy ca làm việc hiện tại của staff
    /// </summary>
    Task<StationStaff?> GetCurrentActiveShiftAsync(int userId);

    /// <summary>
    /// Lấy danh sách staff đang làm việc tại station
    /// </summary>
    Task<IEnumerable<StationStaff>> GetStaffCurrentlyWorkingAtStationAsync(int stationId);

    /// <summary>
    /// Validate shift time hợp lệ
    /// </summary>
    Task ValidateShiftTimingAsync(TimeSpan shiftStart, TimeSpan shiftEnd);

    /// <summary>
    /// Kiểm tra xem station có đủ nhân sự không
    /// </summary>
    Task<bool> HasAdequateCoverageAsync(int stationId);
}
using HRMS.Models;

namespace HRMS.Services;

public interface IAttendanceService
{
    Task<Attendance?> GetByIdAsync(int attendanceId);
    Task<IEnumerable<Attendance>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Attendance>> GetByEmployeeIdAndDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Attendance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Attendance> CreateAsync(Attendance attendance);
    Task<Attendance> UpdateAsync(Attendance attendance);
    Task<bool> DeleteAsync(int attendanceId);
    Task<bool> ExistsAsync(int attendanceId);
    Task<Attendance?> GetCurrentAttendanceAsync(int employeeId);
}


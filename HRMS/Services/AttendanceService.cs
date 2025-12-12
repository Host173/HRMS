using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

public class AttendanceService : IAttendanceService
{
    private readonly HrmsDbContext _context;

    public AttendanceService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<Attendance?> GetByIdAsync(int attendanceId)
    {
        return await _context.Attendance
            .Include(a => a.employee)
            .Include(a => a.shift)
            .Include(a => a.exception)
            .FirstOrDefaultAsync(a => a.attendance_id == attendanceId);
    }

    public async Task<IEnumerable<Attendance>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Attendance
            .Include(a => a.shift)
            .Where(a => a.employee_id == employeeId)
            .OrderByDescending(a => a.entry_time)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByEmployeeIdAndDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        return await _context.Attendance
            .Include(a => a.shift)
            .Where(a => a.employee_id == employeeId &&
                       a.entry_time >= startDate &&
                       a.entry_time <= endDate)
            .OrderByDescending(a => a.entry_time)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Attendance
            .Include(a => a.employee)
            .Include(a => a.shift)
            .Where(a => a.entry_time >= startDate && a.entry_time <= endDate)
            .OrderByDescending(a => a.entry_time)
            .ToListAsync();
    }

    public async Task<Attendance> CreateAsync(Attendance attendance)
    {
        _context.Attendance.Add(attendance);
        await _context.SaveChangesAsync();
        return attendance;
    }

    public async Task<Attendance> UpdateAsync(Attendance attendance)
    {
        _context.Attendance.Update(attendance);
        await _context.SaveChangesAsync();
        return attendance;
    }

    public async Task<bool> DeleteAsync(int attendanceId)
    {
        var attendance = await GetByIdAsync(attendanceId);
        if (attendance == null)
            return false;

        _context.Attendance.Remove(attendance);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int attendanceId)
    {
        return await _context.Attendance
            .AnyAsync(a => a.attendance_id == attendanceId);
    }

    public async Task<Attendance?> GetCurrentAttendanceAsync(int employeeId)
    {
        var today = DateTime.Today;
        return await _context.Attendance
            .Include(a => a.shift)
            .Where(a => a.employee_id == employeeId &&
                       a.entry_time >= today &&
                       a.exit_time == null)
            .OrderByDescending(a => a.entry_time)
            .FirstOrDefaultAsync();
    }
}


using HRMS.Models;

namespace HRMS.Services;

public interface IShiftService
{
    Task<ShiftSchedule?> GetByIdAsync(int shiftId);
    Task<IEnumerable<ShiftSchedule>> GetAllAsync();
    Task<IEnumerable<ShiftSchedule>> GetActiveShiftsAsync();
    Task<IEnumerable<ShiftSchedule>> GetByEmployeeIdAsync(int employeeId);
    Task<ShiftSchedule> CreateAsync(ShiftSchedule shift);
    Task<ShiftSchedule> UpdateAsync(ShiftSchedule shift);
    Task<bool> DeleteAsync(int shiftId);
    Task<bool> ExistsAsync(int shiftId);
}


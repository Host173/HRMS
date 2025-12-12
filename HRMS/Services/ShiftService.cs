using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

public class ShiftService : IShiftService
{
    private readonly HrmsDbContext _context;

    public ShiftService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<ShiftSchedule?> GetByIdAsync(int shiftId)
    {
        return await _context.ShiftSchedule
            .Include(s => s.ShiftAssignment)
            .FirstOrDefaultAsync(s => s.shift_id == shiftId);
    }

    public async Task<IEnumerable<ShiftSchedule>> GetAllAsync()
    {
        return await _context.ShiftSchedule.ToListAsync();
    }

    public async Task<IEnumerable<ShiftSchedule>> GetActiveShiftsAsync()
    {
        return await _context.ShiftSchedule
            .Where(s => s.status == "Active" || s.status == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<ShiftSchedule>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.ShiftSchedule
            .Where(s => s.ShiftAssignment.Any(sa => sa.employee_id == employeeId))
            .ToListAsync();
    }

    public async Task<ShiftSchedule> CreateAsync(ShiftSchedule shift)
    {
        _context.ShiftSchedule.Add(shift);
        await _context.SaveChangesAsync();
        return shift;
    }

    public async Task<ShiftSchedule> UpdateAsync(ShiftSchedule shift)
    {
        _context.ShiftSchedule.Update(shift);
        await _context.SaveChangesAsync();
        return shift;
    }

    public async Task<bool> DeleteAsync(int shiftId)
    {
        var shift = await GetByIdAsync(shiftId);
        if (shift == null)
            return false;

        _context.ShiftSchedule.Remove(shift);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int shiftId)
    {
        return await _context.ShiftSchedule
            .AnyAsync(s => s.shift_id == shiftId);
    }
}


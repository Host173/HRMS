using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

public class MissionService : IMissionService
{
    private readonly HrmsDbContext _context;

    public MissionService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<Mission?> GetByIdAsync(int missionId)
    {
        return await _context.Mission
            .Include(m => m.employee)
            .Include(m => m.manager)
            .FirstOrDefaultAsync(m => m.mission_id == missionId);
    }

    public async Task<IEnumerable<Mission>> GetAllAsync()
    {
        return await _context.Mission
            .Include(m => m.employee)
            .Include(m => m.manager)
            .OrderByDescending(m => m.start_date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Mission>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Mission
            .Include(m => m.manager)
            .Where(m => m.employee_id == employeeId)
            .OrderByDescending(m => m.start_date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Mission>> GetByManagerIdAsync(int managerId)
    {
        return await _context.Mission
            .Include(m => m.employee)
            .Where(m => m.manager_id == managerId)
            .OrderByDescending(m => m.start_date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Mission>> GetActiveMissionsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Mission
            .Include(m => m.employee)
            .Include(m => m.manager)
            .Where(m => m.status == "Active" || 
                       (m.start_date <= today && (m.end_date == null || m.end_date >= today)))
            .OrderByDescending(m => m.start_date)
            .ToListAsync();
    }

    public async Task<Mission> CreateAsync(Mission mission)
    {
        _context.Mission.Add(mission);
        await _context.SaveChangesAsync();
        return mission;
    }

    public async Task<Mission> UpdateAsync(Mission mission)
    {
        _context.Mission.Update(mission);
        await _context.SaveChangesAsync();
        return mission;
    }

    public async Task<bool> DeleteAsync(int missionId)
    {
        var mission = await GetByIdAsync(missionId);
        if (mission == null)
            return false;

        _context.Mission.Remove(mission);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int missionId)
    {
        return await _context.Mission
            .AnyAsync(m => m.mission_id == missionId);
    }
}


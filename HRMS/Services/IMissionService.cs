using HRMS.Models;

namespace HRMS.Services;

public interface IMissionService
{
    Task<Mission?> GetByIdAsync(int missionId);
    Task<IEnumerable<Mission>> GetAllAsync();
    Task<IEnumerable<Mission>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Mission>> GetByManagerIdAsync(int managerId);
    Task<IEnumerable<Mission>> GetActiveMissionsAsync();
    Task<Mission> CreateAsync(Mission mission);
    Task<Mission> UpdateAsync(Mission mission);
    Task<bool> DeleteAsync(int missionId);
    Task<bool> ExistsAsync(int missionId);
}


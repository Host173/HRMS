using HRMS.Models;

namespace HRMS.Services;

public interface IEmployeeService
{
    Task<Employee?> GetByIdAsync(int employeeId);
    Task<Employee?> GetByEmailAsync(string email);
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync();
    Task<Employee> CreateAsync(Employee employee);
    Task<Employee> UpdateAsync(Employee employee);
    Task<bool> DeleteAsync(int employeeId);
    Task<bool> ExistsAsync(int employeeId);
    Task<bool> EmailExistsAsync(string email);
}


using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

public class EmployeeService : IEmployeeService
{
    private readonly HrmsDbContext _context;

    public EmployeeService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(int employeeId)
    {
        return await _context.Employee
            .FirstOrDefaultAsync(e => e.employee_id == employeeId);
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        return await _context.Employee
            .FirstOrDefaultAsync(e => e.email != null && e.email.ToLower() == email.ToLower());
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        return await _context.Employee.ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    {
        return await _context.Employee
            .Where(e => e.is_active == true)
            .ToListAsync();
    }

    public async Task<Employee> CreateAsync(Employee employee)
    {
        _context.Employee.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<Employee> UpdateAsync(Employee employee)
    {
        _context.Employee.Update(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<bool> DeleteAsync(int employeeId)
    {
        var employee = await GetByIdAsync(employeeId);
        if (employee == null)
            return false;

        _context.Employee.Remove(employee);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int employeeId)
    {
        return await _context.Employee
            .AnyAsync(e => e.employee_id == employeeId);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Employee
            .AnyAsync(e => e.email != null && e.email.ToLower() == email.ToLower());
    }
}


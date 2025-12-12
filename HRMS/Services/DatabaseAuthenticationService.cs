using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

/// <summary>
/// Database implementation of IAccountService.
/// Uses Entity Framework to query the Employee table for authentication.
/// </summary>
public class DatabaseAuthenticationService : IAccountService
{
    private readonly HrmsDbContext _context;

    public DatabaseAuthenticationService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ValidateUserAsync(string username, string password)
    {
        // Username is actually email in our system
        var employee = await _context.Employee
            .FirstOrDefaultAsync(e => e.email != null && e.email.ToLower() == username.ToLower() && e.is_active == true);

        if (employee == null || string.IsNullOrEmpty(employee.password_hash))
        {
            return false;
        }

        // Verify password using BCrypt
        return BCrypt.Net.BCrypt.Verify(password, employee.password_hash);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var employee = await _context.Employee
            .FirstOrDefaultAsync(e => e.email != null && e.email.ToLower() == username.ToLower() && e.is_active == true);

        if (employee == null)
        {
            return null;
        }

        return new User
        {
            Id = employee.employee_id,
            Username = employee.email ?? string.Empty,
            Email = employee.email ?? string.Empty,
            FullName = employee.full_name ?? $"{employee.first_name} {employee.last_name}".Trim()
        };
    }

    /// <summary>
    /// Hashes a password using BCrypt
    /// </summary>
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}


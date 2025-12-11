using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

/// <summary>
/// Database implementation of IUserAuthenticationService.
/// Uses Entity Framework to query the Employee table for authentication.
/// </summary>
public class DatabaseAuthenticationService : IUserAuthenticationService
{
    private readonly ApplicationDbContext _context;

    public DatabaseAuthenticationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ValidateUserAsync(string username, string password)
    {
        // Username is actually email in our system
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email != null && e.Email.ToLower() == username.ToLower() && e.IsActive);

        if (employee == null || string.IsNullOrEmpty(employee.PasswordHash))
        {
            return false;
        }

        // Verify password using BCrypt
        return BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email != null && e.Email.ToLower() == username.ToLower() && e.IsActive);

        if (employee == null)
        {
            return null;
        }

        return new User
        {
            Id = employee.EmployeeId,
            Username = employee.Email ?? string.Empty,
            Email = employee.Email ?? string.Empty,
            FullName = employee.FullName ?? $"{employee.FirstName} {employee.LastName}".Trim()
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


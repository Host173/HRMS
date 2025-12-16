using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Helpers;

/// <summary>
/// Helper class for authorization and role management
/// </summary>
public static class AuthorizationHelper
{
    // Role name constants
    public const string SystemAdminRole = "System Administrator";
    public const string HRAdminRole = "HR Administrator";
    public const string LineManagerRole = "Line Manager";
    public const string EmployeeRole = "Employee";

    /// <summary>
    /// Gets the current employee ID from the user's claims
    /// </summary>
    public static int? GetCurrentEmployeeId(ClaimsPrincipal? user)
    {
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            return null;
        }

        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !int.TryParse(nameIdentifier, out var employeeId))
        {
            return null;
        }

        return employeeId;
    }

    /// <summary>
    /// Checks if an employee has a specific role
    /// </summary>
    public static async Task<bool> HasRoleAsync(HrmsDbContext context, int employeeId, string roleName)
    {
        return await context.Employee_Role
            .Include(er => er.role)
            .AnyAsync(er => er.employee_id == employeeId && er.role.role_name == roleName);
    }

    /// <summary>
    /// Checks if an employee is a System Administrator
    /// </summary>
    public static async Task<bool> IsSystemAdminAsync(HrmsDbContext context, int employeeId)
    {
        return await HasRoleAsync(context, employeeId, SystemAdminRole);
    }

    /// <summary>
    /// Checks if an employee is an HR Administrator
    /// </summary>
    public static async Task<bool> IsHRAdminAsync(HrmsDbContext context, int employeeId)
    {
        return await HasRoleAsync(context, employeeId, HRAdminRole);
    }

    /// <summary>
    /// Checks if an employee is a Line Manager
    /// </summary>
    public static async Task<bool> IsLineManagerAsync(HrmsDbContext context, int employeeId)
    {
        return await HasRoleAsync(context, employeeId, LineManagerRole);
    }

    /// <summary>
    /// Gets all roles for an employee
    /// </summary>
    public static async Task<List<string>> GetEmployeeRolesAsync(HrmsDbContext context, int employeeId)
    {
        return await context.Employee_Role
            .Include(er => er.role)
            .Where(er => er.employee_id == employeeId)
            .Select(er => er.role.role_name)
            .ToListAsync();
    }

    /// <summary>
    /// Assigns a role to an employee
    /// </summary>
    public static async Task AssignRoleAsync(HrmsDbContext context, int employeeId, string roleName)
    {
        // Check if role already assigned
        var hasRole = await HasRoleAsync(context, employeeId, roleName);
        if (hasRole)
        {
            return; // Already has the role
        }

        // Find the role by name
        var role = await context.Role
            .FirstOrDefaultAsync(r => r.role_name == roleName);

        if (role == null)
        {
            throw new InvalidOperationException($"Role '{roleName}' not found in database.");
        }

        // Create new employee role assignment
        var employeeRole = new Employee_Role
        {
            employee_id = employeeId,
            role_id = role.role_id,
            assigned_date = DateOnly.FromDateTime(DateTime.Now)
        };

        context.Employee_Role.Add(employeeRole);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a role from an employee
    /// </summary>
    public static async Task RemoveRoleAsync(HrmsDbContext context, int employeeId, string roleName)
    {
        var employeeRole = await context.Employee_Role
            .Include(er => er.role)
            .FirstOrDefaultAsync(er => er.employee_id == employeeId && er.role.role_name == roleName);

        if (employeeRole != null)
        {
            context.Employee_Role.Remove(employeeRole);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets list of employees that can be assigned as managers (excluding System Admins, HR Admins, and Line Managers)
    /// </summary>
    public static async Task<List<Employee>> GetAssignableEmployeesAsync(HrmsDbContext context, int? excludeEmployeeId = null)
    {
        // Get IDs of employees who have System Admin, HR Admin, or Line Manager roles
        var restrictedEmployeeIds = await context.Employee_Role
            .Include(er => er.role)
            .Where(er => er.role.role_name == SystemAdminRole ||
                        er.role.role_name == HRAdminRole ||
                        er.role.role_name == LineManagerRole)
            .Select(er => er.employee_id)
            .Distinct()
            .ToListAsync();

        var query = context.Employee
            .Where(e => e.is_active == true && !restrictedEmployeeIds.Contains(e.employee_id));

        if (excludeEmployeeId.HasValue)
        {
            query = query.Where(e => e.employee_id != excludeEmployeeId.Value);
        }

        return await query
            .OrderBy(e => e.full_name)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if an employee can be managed by a Line Manager
    /// (i.e., they don't have System Admin, HR Admin, or Line Manager roles)
    /// </summary>
    public static async Task<bool> CanBeManagedByLineManagerAsync(HrmsDbContext context, int employeeId)
    {
        var hasRestrictedRole = await context.Employee_Role
            .Include(er => er.role)
            .AnyAsync(er => er.employee_id == employeeId &&
                          (er.role.role_name == SystemAdminRole ||
                           er.role.role_name == HRAdminRole ||
                           er.role.role_name == LineManagerRole));

        return !hasRestrictedRole;
    }
}

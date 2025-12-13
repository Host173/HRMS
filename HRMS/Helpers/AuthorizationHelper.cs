using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Helpers;

public static class AuthorizationHelper
{
    public const string SystemAdminRole = "System Administrator";
    public const string HRAdminRole = "HR Administrator";
    public const string LineManagerRole = "Line Manager";
    public const string EmployeeRole = "Employee";

    /// <summary>
    /// Gets the current user's employee ID from claims
    /// </summary>
    public static int? GetCurrentEmployeeId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var employeeId))
        {
            return employeeId;
        }
        return null;
    }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    public static async Task<bool> HasRoleAsync(HrmsDbContext context, int employeeId, string roleName)
    {
        return await context.Employee_Role
            .Include(er => er.role)
            .AnyAsync(er => er.employee_id == employeeId && er.role.role_name == roleName);
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
    /// Checks if user is System Admin
    /// </summary>
    public static async Task<bool> IsSystemAdminAsync(HrmsDbContext context, int employeeId)
    {
        return await HasRoleAsync(context, employeeId, SystemAdminRole);
    }

    /// <summary>
    /// Checks if user is HR Admin
    /// </summary>
    public static async Task<bool> IsHRAdminAsync(HrmsDbContext context, int employeeId)
    {
        return await HasRoleAsync(context, employeeId, HRAdminRole);
    }

    /// <summary>
    /// Checks if user is Line Manager
    /// </summary>
    public static async Task<bool> IsLineManagerAsync(HrmsDbContext context, int employeeId)
    {
        return await HasRoleAsync(context, employeeId, LineManagerRole);
    }

    /// <summary>
    /// Checks if user is System Admin or HR Admin
    /// </summary>
    public static async Task<bool> IsAdminAsync(HrmsDbContext context, int employeeId)
    {
        return await IsSystemAdminAsync(context, employeeId) || 
               await IsHRAdminAsync(context, employeeId);
    }

    /// <summary>
    /// Checks if an employee has Manager, HR Admin, or System Admin role
    /// (Used to restrict managers from assigning these roles to their team)
    /// </summary>
    public static async Task<bool> IsManagerOrHRAdminAsync(HrmsDbContext context, int employeeId)
    {
        return await IsLineManagerAsync(context, employeeId) || 
               await IsHRAdminAsync(context, employeeId) ||
               await IsSystemAdminAsync(context, employeeId);
    }

    /// <summary>
    /// Checks if an employee can be managed by a Line Manager
    /// (Only employees without Manager, HR Admin, or System Admin roles can be managed by Line Managers)
    /// </summary>
    public static async Task<bool> CanBeManagedByLineManagerAsync(HrmsDbContext context, int employeeId)
    {
        // Employee can be managed by a Line Manager if they don't have Manager, HR Admin, or System Admin role
        return !await IsManagerOrHRAdminAsync(context, employeeId);
    }

    /// <summary>
    /// Gets all employees who can be assigned to a Line Manager's team
    /// (Excludes employees with Manager, HR Admin, or System Admin roles)
    /// </summary>
    public static async Task<List<Employee>> GetAssignableEmployeesAsync(HrmsDbContext context, int? excludeEmployeeId = null)
    {
        // Get all active employees
        var employees = await context.Employee
            .Include(e => e.department)
            .Include(e => e.position)
            .Include(e => e.Employee_Role)
                .ThenInclude(er => er.role)
            .Where(e => e.is_active == true)
            .ToListAsync();

        // Filter out employees who have Manager, HR Admin, or System Admin roles
        var assignableEmployees = new List<Employee>();
        foreach (var employee in employees)
        {
            // Exclude the specified employee (usually the current manager)
            if (excludeEmployeeId.HasValue && employee.employee_id == excludeEmployeeId.Value)
                continue;

            // Check if employee has Manager, HR Admin, or System Admin role
            bool hasRestrictedRole = await IsManagerOrHRAdminAsync(context, employee.employee_id);
            if (!hasRestrictedRole)
            {
                assignableEmployees.Add(employee);
            }
        }

        return assignableEmployees;
    }

    /// <summary>
    /// Assigns a role to an employee
    /// </summary>
    public static async Task AssignRoleAsync(HrmsDbContext context, int employeeId, string roleName)
    {
        // Check if role exists, create it if it doesn't
        var role = await context.Role
            .FirstOrDefaultAsync(r => r.role_name == roleName);

        if (role == null)
        {
            // Create the role if it doesn't exist
            role = new Role
            {
                role_name = roleName,
                purpose = $"System role: {roleName}"
            };
            context.Role.Add(role);
            await context.SaveChangesAsync();
        }

        // Check if employee already has this role
        var existingRole = await context.Employee_Role
            .FirstOrDefaultAsync(er => er.employee_id == employeeId && er.role_id == role.role_id);

        if (existingRole == null)
        {
            var employeeRole = new Employee_Role
            {
                employee_id = employeeId,
                role_id = role.role_id,
                assigned_date = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            context.Employee_Role.Add(employeeRole);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Removes a role from an employee
    /// </summary>
    public static async Task RemoveRoleAsync(HrmsDbContext context, int employeeId, string roleName)
    {
        var role = await context.Role
            .FirstOrDefaultAsync(r => r.role_name == roleName);

        if (role != null)
        {
            var employeeRole = await context.Employee_Role
                .FirstOrDefaultAsync(er => er.employee_id == employeeId && er.role_id == role.role_id);

            if (employeeRole != null)
            {
                context.Employee_Role.Remove(employeeRole);
                await context.SaveChangesAsync();
            }
        }
    }
}


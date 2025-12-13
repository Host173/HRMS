using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;

namespace HRMS.Controllers;

[Authorize]
public class DepartmentController : Controller
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(HrmsDbContext context, ILogger<DepartmentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List all departments (System Admin only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
        if (!isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to manage departments.";
            return RedirectToAction("Index", "Home");
        }

        var departments = await _context.Department
            .Include(d => d.Employee)
            .Include(d => d.department_head)
            .OrderBy(d => d.department_name)
            .ToListAsync();

        return View(departments);
    }

    /// <summary>
    /// Create new department (System Admin only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
        if (!isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to create departments.";
            return RedirectToAction("Index", "Home");
        }

        // Get all employees for department head selection
        var employees = await _context.Employee
            .Where(e => e.is_active == true)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        ViewBag.Employees = employees;

        return View();
    }

    /// <summary>
    /// Create new department (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string departmentName, string? purpose, int? departmentHeadId)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
        if (!isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to create departments.";
            return RedirectToAction("Index", "Home");
        }

        if (string.IsNullOrWhiteSpace(departmentName))
        {
            TempData["ErrorMessage"] = "Department name is required.";
            var employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.Employees = employees;
            return View();
        }

        // Check if department with same name already exists
        var existingDept = await _context.Department
            .FirstOrDefaultAsync(d => d.department_name.ToLower() == departmentName.Trim().ToLower());

        if (existingDept != null)
        {
            TempData["ErrorMessage"] = "A department with this name already exists.";
            var employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.Employees = employees;
            return View();
        }

        try
        {
            var department = new Department
            {
                department_name = departmentName.Trim(),
                purpose = string.IsNullOrWhiteSpace(purpose) ? null : purpose.Trim(),
                department_head_id = departmentHeadId
            };

            _context.Department.Add(department);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Department created: {DepartmentName} by System Admin {AdminId}", 
                department.department_name, employeeId.Value);

            TempData["SuccessMessage"] = $"Department '{department.department_name}' created successfully.";
            return RedirectToAction("Index");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            TempData["ErrorMessage"] = "An error occurred while creating the department.";
            var employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.Employees = employees;
            return View();
        }
    }

    /// <summary>
    /// Assign people to department (System Admin only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AssignToDepartment()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
        if (!isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to assign people to departments.";
            return RedirectToAction("Index", "Home");
        }

        var departments = await _context.Department
            .OrderBy(d => d.department_name)
            .ToListAsync();

        var allPeople = await _context.Employee
            .Where(e => e.is_active == true)
            .Include(e => e.department)
            .Include(e => e.Employee_Role)
                .ThenInclude(er => er.role)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        ViewBag.Departments = departments;
        ViewBag.AllPeople = allPeople;

        return View();
    }

    /// <summary>
    /// Assign people to department (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToDepartment(int personId, int departmentId)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
        if (!isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to assign people to departments.";
            return RedirectToAction("Index", "Home");
        }

        var person = await _context.Employee.FindAsync(personId);
        if (person == null)
        {
            TempData["ErrorMessage"] = "Person not found.";
            return RedirectToAction("AssignToDepartment");
        }

        var department = await _context.Department.FindAsync(departmentId);
        if (department == null)
        {
            TempData["ErrorMessage"] = "Department not found.";
            return RedirectToAction("AssignToDepartment");
        }

        try
        {
            person.department_id = departmentId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Person {PersonId} assigned to department {DepartmentId} by System Admin {AdminId}", 
                personId, departmentId, employeeId.Value);

            TempData["SuccessMessage"] = $"{person.full_name ?? $"{person.first_name} {person.last_name}"} has been assigned to {department.department_name}.";
            return RedirectToAction("AssignToDepartment");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error assigning person to department");
            TempData["ErrorMessage"] = "An error occurred while assigning the person to the department.";
            return RedirectToAction("AssignToDepartment");
        }
    }

    /// <summary>
    /// View department members - anyone in the same department can see all members
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ViewMembers(int? departmentId = null)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var currentEmployee = await _context.Employee
            .Include(e => e.department)
            .FirstOrDefaultAsync(e => e.employee_id == employeeId.Value);

        if (currentEmployee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // If no departmentId specified, use current employee's department
        var targetDepartmentId = departmentId ?? currentEmployee.department_id;

        if (!targetDepartmentId.HasValue)
        {
            TempData["ErrorMessage"] = "You are not assigned to any department, or the specified department does not exist.";
            return RedirectToAction("Index", "Home");
        }

        // Verify the user is in the same department (unless they're System Admin or HR Admin)
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);

        if (!isSystemAdmin && !isHRAdmin && currentEmployee.department_id != targetDepartmentId)
        {
            TempData["ErrorMessage"] = "You can only view members of your own department.";
            return RedirectToAction("Index", "Home");
        }

        var department = await _context.Department
            .Include(d => d.department_head)
            .FirstOrDefaultAsync(d => d.department_id == targetDepartmentId.Value);

        if (department == null)
        {
            TempData["ErrorMessage"] = "Department not found.";
            return RedirectToAction("Index", "Home");
        }

        // Get all people in this department (employees, HR admins, system admins, line managers)
        var departmentMembers = await _context.Employee
            .Where(e => e.department_id == targetDepartmentId.Value && e.is_active == true)
            .Include(e => e.position)
            .Include(e => e.Employee_Role)
                .ThenInclude(er => er.role)
            .Include(e => e.department)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        ViewBag.Department = department;
        ViewBag.IsSystemAdmin = isSystemAdmin;
        ViewBag.IsHRAdmin = isHRAdmin;

        return View(departmentMembers);
    }

    /// <summary>
    /// Delete department (System Admin only)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
        if (!isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to delete departments.";
            return RedirectToAction("Index");
        }

        var department = await _context.Department
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.department_id == id);

        if (department == null)
        {
            TempData["ErrorMessage"] = "Department not found.";
            return RedirectToAction("Index");
        }

        // Check if department has employees
        if (department.Employee.Any(e => e.is_active == true))
        {
            TempData["ErrorMessage"] = $"Cannot delete department '{department.department_name}' because it has active employees. Please reassign employees first.";
            return RedirectToAction("Index");
        }

        try
        {
            _context.Department.Remove(department);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Department deleted: {DepartmentName} by System Admin {AdminId}", 
                department.department_name, employeeId.Value);

            TempData["SuccessMessage"] = $"Department '{department.department_name}' deleted successfully.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error deleting department");
            TempData["ErrorMessage"] = "An error occurred while deleting the department.";
        }

        return RedirectToAction("Index");
    }
}


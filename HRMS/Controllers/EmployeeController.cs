using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;
using HRMS.Services;
using HRMS.Helpers;

namespace HRMS.Controllers;

[Authorize]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<EmployeeController> _logger;
    private readonly IWebHostEnvironment _environment;

    public EmployeeController(
        IEmployeeService employeeService,
        HrmsDbContext context,
        ILogger<EmployeeController> logger,
        IWebHostEnvironment environment)
    {
        _employeeService = employeeService;
        _context = context;
        _logger = logger;
        _environment = environment;
    }
    
    /// <summary>
    /// System Admins, HR Admins, and Line Managers can view all employees in all departments
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Check if user is System Admin, HR Admin, or Line Manager
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, currentEmployeeId.Value);
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
        var isLineManager = await AuthorizationHelper.IsLineManagerAsync(_context, currentEmployeeId.Value);

        if (!isSystemAdmin && !isHRAdmin && !isLineManager)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var employees = await _context.Employee
            .Include(e => e.department)
            .Include(e => e.position)
            .Include(e => e.Employee_Role)
                .ThenInclude(er => er.role)
            .Where(e => e.is_active == true)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        return View(employees);
    }

    /// <summary>
    /// Admins and Managers can view full employee profiles
    /// Employees can view their own profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var employee = await _context.Employee
            .Include(e => e.department)
            .Include(e => e.position)
            .Include(e => e.manager)
            .Include(e => e.contract)
            .Include(e => e.Employee_Role)
                .ThenInclude(er => er.role)
            .FirstOrDefaultAsync(e => e.employee_id == id);

        if (employee == null)
        {
            return NotFound();
        }

        // Check permissions
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, currentEmployeeId.Value);
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
        var isLineManager = await AuthorizationHelper.IsLineManagerAsync(_context, currentEmployeeId.Value);
        var isOwnProfile = currentEmployeeId.Value == id;

        // System Admins, HR Admins, and Line Managers can view full profiles of any employee
        // Line Managers can view profiles of all employees, not just their team members
        if (!isSystemAdmin && !isHRAdmin && !isLineManager && !isOwnProfile)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewBag.CanEdit = isSystemAdmin || isHRAdmin || isOwnProfile;
        ViewBag.IsHRAdmin = isHRAdmin;
        ViewBag.IsSystemAdmin = isSystemAdmin;

        return View(employee);
    }

    /// <summary>
    /// Employees can update personal details and emergency contacts
    /// HR Admins can edit any part of employee profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var employee = await _employeeService.GetByIdAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        // Check permissions
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
        var isOwnProfile = currentEmployeeId.Value == id;

        if (!isHRAdmin && !isOwnProfile)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Load related data for dropdowns
        ViewBag.Departments = await _context.Department.ToListAsync();
        ViewBag.Positions = await _context.Position.ToListAsync();
        
        // Get potential managers - HR Admins can assign anyone, others should only see assignable employees
        if (isHRAdmin)
        {
            // HR Admins can assign any active employee as manager
        ViewBag.Managers = await _context.Employee
            .Where(e => e.is_active == true && e.employee_id != id)
            .ToListAsync();
        }
        else
        {
            // Non-HR users should only see employees who can be managed (excluding Managers, HR Admins, and System Admins)
            ViewBag.Managers = await AuthorizationHelper.GetAssignableEmployeesAsync(_context, id);
        }
        
        ViewBag.Roles = await _context.Role.ToListAsync();
        ViewBag.IsHRAdmin = isHRAdmin;
        ViewBag.IsSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, currentEmployeeId.Value);

        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee employee, IFormFile? profileImage)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Check permissions
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
        var isOwnProfile = currentEmployeeId.Value == id;

        if (!isHRAdmin && !isOwnProfile)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (id != employee.employee_id)
        {
            return NotFound();
        }

        var existingEmployee = await _employeeService.GetByIdAsync(id);
        if (existingEmployee == null)
        {
            return NotFound();
        }

        // Handle profile image upload
        if (profileImage != null && profileImage.Length > 0)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{employee.employee_id}_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(stream);
            }

            employee.profile_image = $"/uploads/profiles/{fileName}";
        }
        else
        {
            // Keep existing image if no new image uploaded
            employee.profile_image = existingEmployee.profile_image;
        }

        // Update only allowed fields based on role
        if (isHRAdmin)
        {
            // HR Admin can edit everything
            existingEmployee.first_name = employee.first_name;
            existingEmployee.last_name = employee.last_name;
            existingEmployee.full_name = employee.full_name;
            existingEmployee.email = employee.email;
            existingEmployee.phone = employee.phone;
            existingEmployee.national_id = employee.national_id;
            existingEmployee.date_of_birth = employee.date_of_birth;
            existingEmployee.country_of_birth = employee.country_of_birth;
            existingEmployee.address = employee.address;
            existingEmployee.department_id = employee.department_id;
            existingEmployee.position_id = employee.position_id;
            existingEmployee.manager_id = employee.manager_id;
            existingEmployee.profile_image = employee.profile_image;
        }
        else
        {
            // Employees can only update personal details and emergency contacts
            // Non-HR users cannot change manager_id
            existingEmployee.first_name = employee.first_name;
            existingEmployee.last_name = employee.last_name;
            existingEmployee.full_name = $"{employee.first_name} {employee.last_name}".Trim();
            existingEmployee.phone = employee.phone;
            existingEmployee.address = employee.address;
            existingEmployee.profile_image = employee.profile_image;
        }

        // Always allow emergency contact updates
        existingEmployee.emergency_contact_name = employee.emergency_contact_name;
        existingEmployee.emergency_contact_phone = employee.emergency_contact_phone;
        existingEmployee.relationship = employee.relationship;

        // Calculate profile completion
        existingEmployee.profile_completion = CalculateProfileCompletion(existingEmployee);

        try
        {
            await _employeeService.UpdateAsync(existingEmployee);
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error updating employee profile: {EmployeeId}", id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the profile.");
            
            // Reload related data
            ViewBag.Departments = await _context.Department.ToListAsync();
            ViewBag.Positions = await _context.Position.ToListAsync();
            
            // Get potential managers - HR Admins can assign anyone, others should only see assignable employees
            if (isHRAdmin)
            {
                // HR Admins can assign any active employee as manager
            ViewBag.Managers = await _context.Employee
                .Where(e => e.is_active == true && e.employee_id != id)
                .ToListAsync();
            }
            else
            {
                // Non-HR users should only see employees who can be managed (excluding Managers, HR Admins, and System Admins)
                ViewBag.Managers = await AuthorizationHelper.GetAssignableEmployeesAsync(_context, id);
            }
            
            ViewBag.Roles = await _context.Role.ToListAsync();
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, currentEmployeeId.Value);
            
            return View(existingEmployee);
        }
    }

    /// <summary>
    /// System Admins can assign system roles
    /// </summary>
    [HttpPost]
    [RequireRole(AuthorizationHelper.SystemAdminRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(int employeeId, string roleName)
    {
        try
        {
            await AuthorizationHelper.AssignRoleAsync(_context, employeeId, roleName);
            TempData["SuccessMessage"] = $"Role '{roleName}' assigned successfully.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error assigning role: {Role} to employee: {EmployeeId}", roleName, employeeId);
            TempData["ErrorMessage"] = "An error occurred while assigning the role.";
        }

        return RedirectToAction(nameof(Details), new { id = employeeId });
    }

    [HttpPost]
    [RequireRole(AuthorizationHelper.SystemAdminRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(int employeeId, string roleName)
    {
        try
        {
            await AuthorizationHelper.RemoveRoleAsync(_context, employeeId, roleName);
            TempData["SuccessMessage"] = $"Role '{roleName}' removed successfully.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error removing role: {Role} from employee: {EmployeeId}", roleName, employeeId);
            TempData["ErrorMessage"] = "An error occurred while removing the role.";
        }

        return RedirectToAction(nameof(Details), new { id = employeeId });
    }

    /// <summary>
    /// Employees can view their team (colleagues who share the same manager)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyTeam()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var currentEmployee = await _employeeService.GetByIdAsync(currentEmployeeId.Value);
        if (currentEmployee == null)
        {
            return NotFound();
        }

        var isLineManager = await AuthorizationHelper.IsLineManagerAsync(_context, currentEmployeeId.Value);

        IEnumerable<Employee> teamMembers;

        if (isLineManager)
        {
            // Line Managers see their direct reports (team members they manage)
            teamMembers = await _context.Employee
                .Include(e => e.department)
                .Include(e => e.position)
                .Where(e => e.manager_id == currentEmployeeId.Value && e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            
            ViewBag.IsManager = true;
            ViewBag.TeamType = "My Team (Direct Reports)";
        }
        else
        {
            // Regular employees see their colleagues (team members who share the same manager)
            if (currentEmployee.manager_id.HasValue)
            {
                teamMembers = await _context.Employee
                    .Include(e => e.department)
                    .Include(e => e.position)
                    .Where(e => e.manager_id == currentEmployee.manager_id.Value && 
                           e.is_active == true && 
                           e.employee_id != currentEmployeeId.Value) // Exclude self
                    .OrderBy(e => e.full_name)
                    .ToListAsync();
                
                // Get manager info
                var manager = await _employeeService.GetByIdAsync(currentEmployee.manager_id.Value);
                ViewBag.Manager = manager;
                ViewBag.IsManager = false;
                ViewBag.TeamType = "My Team (Colleagues)";
            }
            else
            {
                teamMembers = new List<Employee>();
                ViewBag.IsManager = false;
                ViewBag.TeamType = "My Team";
            }
        }

        return View(teamMembers);
    }

    /// <summary>
    /// Line Managers can view full profiles of their team members
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    public async Task<IActionResult> ViewTeamProfiles()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var teamMembers = await _context.Employee
            .Include(e => e.department)
            .Include(e => e.position)
            .Include(e => e.contract)
            .Include(e => e.Employee_Role)
                .ThenInclude(er => er.role)
            .Where(e => e.manager_id == currentEmployeeId.Value && e.is_active == true)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        return View(teamMembers);
    }

    /// <summary>
    /// Line Managers can assign employees to their team
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    public async Task<IActionResult> AssignToTeam()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Get employees that can be assigned to this manager
        // Line Managers can only assign employees without Manager, HR Admin, or System Admin roles
        var assignableEmployees = await AuthorizationHelper.GetAssignableEmployeesAsync(_context, currentEmployeeId.Value);
        
        // Filter to only show employees not already assigned to this manager
        var availableEmployees = assignableEmployees
            .Where(e => e.manager_id == null || e.manager_id != currentEmployeeId.Value)
            .OrderBy(e => e.full_name)
            .ToList();

        // Get current team members
        var currentTeam = await _context.Employee
            .Where(e => e.manager_id == currentEmployeeId.Value && e.is_active == true)
            .Select(e => e.employee_id)
            .ToListAsync();

        ViewBag.AvailableEmployees = availableEmployees;
        ViewBag.CurrentTeam = currentTeam;
        ViewBag.CurrentManagerId = currentEmployeeId.Value;

        return View();
    }

    /// <summary>
    /// Assign employee to manager's team
    /// </summary>
    [HttpPost]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToTeam(int employeeId)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var employee = await _employeeService.GetByIdAsync(employeeId);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Employee not found.";
            return RedirectToAction(nameof(AssignToTeam));
        }

        // Check if the employee can be managed by a Line Manager
        // Line Managers cannot assign employees with Manager, HR Admin, or System Admin roles
        var canBeManaged = await AuthorizationHelper.CanBeManagedByLineManagerAsync(_context, employeeId);
        if (!canBeManaged)
        {
            _logger.LogWarning("Manager {ManagerId} attempted to assign employee {EmployeeId} who has Manager, HR Admin, or System Admin role", 
                currentEmployeeId.Value, employeeId);
            TempData["ErrorMessage"] = "You cannot assign employees with Manager, HR Admin, or System Admin roles. Only HR Admins can manage such assignments.";
            return RedirectToAction(nameof(AssignToTeam));
        }

        if (employee.manager_id == currentEmployeeId.Value)
        {
            TempData["InfoMessage"] = "Employee is already assigned to your team.";
            return RedirectToAction(nameof(AssignToTeam));
        }

        try
        {
            employee.manager_id = currentEmployeeId.Value;
            await _employeeService.UpdateAsync(employee);

            _logger.LogInformation("Employee {EmployeeId} assigned to manager {ManagerId}", employeeId, currentEmployeeId.Value);
            TempData["SuccessMessage"] = $"Employee {employee.full_name} has been successfully assigned to your team.";
            return RedirectToAction(nameof(MyTeam));
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error assigning employee {EmployeeId} to manager {ManagerId}", employeeId, currentEmployeeId.Value);
            TempData["ErrorMessage"] = "An error occurred while assigning the employee to your team.";
            return RedirectToAction(nameof(AssignToTeam));
        }
    }

    /// <summary>
    /// Remove employee from manager's team
    /// </summary>
    [HttpPost]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromTeam(int employeeId)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var employee = await _employeeService.GetByIdAsync(employeeId);
        if (employee == null || employee.manager_id != currentEmployeeId.Value)
        {
            TempData["ErrorMessage"] = "Employee not found or not assigned to your team.";
            return RedirectToAction(nameof(MyTeam));
        }

        try
        {
            employee.manager_id = null;
            await _employeeService.UpdateAsync(employee);

            _logger.LogInformation("Employee {EmployeeId} removed from manager {ManagerId}'s team", employeeId, currentEmployeeId.Value);
            TempData["SuccessMessage"] = $"Employee {employee.full_name} has been removed from your team.";
            return RedirectToAction(nameof(MyTeam));
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error removing employee {EmployeeId} from manager {ManagerId}'s team", employeeId, currentEmployeeId.Value);
            TempData["ErrorMessage"] = "An error occurred while removing the employee from your team.";
            return RedirectToAction(nameof(MyTeam));
        }
    }

    /// <summary>
    /// HR Admin can manage profile completeness
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    public async Task<IActionResult> ProfileCompleteness()
    {
        var employees = await _context.Employee
            .Include(e => e.department)
            .Where(e => e.is_active == true)
            .OrderByDescending(e => e.profile_completion ?? 0)
            .ToListAsync();

        return View(employees);
    }

    /// <summary>
    /// Calculate profile completion percentage
    /// </summary>
    private int CalculateProfileCompletion(Employee employee)
    {
        int totalFields = 12;
        int completedFields = 0;

        if (!string.IsNullOrEmpty(employee.first_name)) completedFields++;
        if (!string.IsNullOrEmpty(employee.last_name)) completedFields++;
        if (!string.IsNullOrEmpty(employee.email)) completedFields++;
        if (!string.IsNullOrEmpty(employee.phone)) completedFields++;
        if (!string.IsNullOrEmpty(employee.national_id)) completedFields++;
        if (employee.date_of_birth.HasValue) completedFields++;
        if (!string.IsNullOrEmpty(employee.address)) completedFields++;
        if (!string.IsNullOrEmpty(employee.emergency_contact_name)) completedFields++;
        if (!string.IsNullOrEmpty(employee.emergency_contact_phone)) completedFields++;
        if (employee.department_id.HasValue) completedFields++;
        if (employee.position_id.HasValue) completedFields++;
        if (!string.IsNullOrEmpty(employee.profile_image)) completedFields++;

        return (int)Math.Round((double)completedFields / totalFields * 100);
    }
}


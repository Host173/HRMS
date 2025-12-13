using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;
using HRMS.Services;
using HRMS.Helpers;

namespace HRMS.Controllers;

[Authorize]
public class MissionController : Controller
{
    private readonly IMissionService _missionService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<MissionController> _logger;

    public MissionController(
        IMissionService missionService,
        HrmsDbContext context,
        ILogger<MissionController> logger)
    {
        _missionService = missionService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Employee-side view: Shows only missions assigned to the logged-in employee (read-only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            _logger.LogWarning("No employee ID found in claims, redirecting to login");
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("Employee {EmployeeId} accessing My Missions page", currentEmployeeId.Value);

        // Get missions assigned to the current employee
        // NOTE: Any logged-in employee can view their assigned missions, regardless of role
        // This allows managers/admins to see missions assigned to them as employees
        var missions = await _missionService.GetByEmployeeIdAsync(currentEmployeeId.Value);
        
        _logger.LogInformation("Found {MissionCount} missions for employee {EmployeeId}", 
            missions.Count(), currentEmployeeId.Value);

        return View(missions);
    }

    /// <summary>
    /// Manager-side view: Shows missions where the current manager is responsible for approval
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    public async Task<IActionResult> Manager()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            _logger.LogWarning("No employee ID found in claims for manager, redirecting to login");
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("Manager {EmployeeId} accessing Mission Approvals page", currentEmployeeId.Value);

        // Get missions where current user is the approving manager
        var missions = await _missionService.GetByManagerIdAsync(currentEmployeeId.Value);
        
        _logger.LogInformation("Found {MissionCount} missions for manager {EmployeeId}", 
            missions.Count(), currentEmployeeId.Value);

        // Filter to show only pending/assigned missions (actionable missions)
        // Also include all missions for context
        var allMissions = missions.ToList();
        var pendingMissions = allMissions.Where(m => 
            (m.status ?? "").Equals("Pending", StringComparison.OrdinalIgnoreCase) || 
            (m.status ?? "").Equals("Assigned", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        _logger.LogInformation("Manager {EmployeeId} has {PendingCount} pending missions out of {TotalCount} total", 
            currentEmployeeId.Value, pendingMissions.Count, allMissions.Count);

        ViewBag.PendingMissions = pendingMissions;
        ViewBag.AllMissions = allMissions;

        return View(allMissions);
    }

    /// <summary>
    /// Approve a mission (Manager action)
    /// </summary>
    [HttpPost]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var mission = await _missionService.GetByIdAsync(id);
        if (mission == null)
        {
            TempData["ErrorMessage"] = "Mission not found.";
            return RedirectToAction(nameof(Manager));
        }

        // Verify the current user is the manager for this mission
        if (mission.manager_id != currentEmployeeId.Value)
        {
            TempData["ErrorMessage"] = "You are not authorized to approve this mission.";
            return RedirectToAction(nameof(Manager));
        }

        // Only allow approval of pending/assigned missions
        var status = mission.status ?? "";
        if (!status.Equals("Pending", StringComparison.OrdinalIgnoreCase) && 
            !status.Equals("Assigned", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Only pending or assigned missions can be approved.";
            return RedirectToAction(nameof(Manager));
        }

        try
        {
            mission.status = "Active";
            await _missionService.UpdateAsync(mission);

            _logger.LogInformation("Mission {MissionId} approved by Manager {ManagerId}", id, currentEmployeeId.Value);
            TempData["SuccessMessage"] = $"Mission to {mission.destination ?? "destination"} has been approved successfully.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error approving mission {MissionId}", id);
            TempData["ErrorMessage"] = "An error occurred while approving the mission. Please try again.";
        }

        return RedirectToAction(nameof(Manager));
    }

    /// <summary>
    /// Reject a mission (Manager action)
    /// </summary>
    [HttpPost]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var mission = await _missionService.GetByIdAsync(id);
        if (mission == null)
        {
            TempData["ErrorMessage"] = "Mission not found.";
            return RedirectToAction(nameof(Manager));
        }

        // Verify the current user is the manager for this mission
        if (mission.manager_id != currentEmployeeId.Value)
        {
            TempData["ErrorMessage"] = "You are not authorized to reject this mission.";
            return RedirectToAction(nameof(Manager));
        }

        // Only allow rejection of pending/assigned missions
        var status = mission.status ?? "";
        if (!status.Equals("Pending", StringComparison.OrdinalIgnoreCase) && 
            !status.Equals("Assigned", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Only pending or assigned missions can be rejected.";
            return RedirectToAction(nameof(Manager));
        }

        try
        {
            mission.status = "Cancelled";
            await _missionService.UpdateAsync(mission);

            _logger.LogInformation("Mission {MissionId} rejected by Manager {ManagerId}", id, currentEmployeeId.Value);
            TempData["SuccessMessage"] = $"Mission to {mission.destination ?? "destination"} has been rejected.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error rejecting mission {MissionId}", id);
            TempData["ErrorMessage"] = "An error occurred while rejecting the mission. Please try again.";
        }

        return RedirectToAction(nameof(Manager));
    }

    /// <summary>
    /// HR Admin: Show form to assign a new mission to an employee
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    public async Task<IActionResult> Create()
    {
        _logger.LogInformation("HR Admin accessing mission creation form");

        // Get IDs of employees who have admin or manager roles
        var adminManagerEmployeeIds = await _context.Employee_Role
            .Include(er => er.role)
            .Where(er => er.role.role_name == AuthorizationHelper.SystemAdminRole ||
                        er.role.role_name == AuthorizationHelper.HRAdminRole ||
                        er.role.role_name == AuthorizationHelper.LineManagerRole)
            .Select(er => er.employee_id)
            .Distinct()
            .ToListAsync();

        _logger.LogInformation("Found {Count} employees with admin/manager roles", adminManagerEmployeeIds.Count);

        // Get all active employees EXCLUDING admins and managers
        var eligibleEmployees = await _context.Employee
            .Where(e => e.is_active == true && !adminManagerEmployeeIds.Contains(e.employee_id))
            .OrderBy(e => e.full_name)
            .ToListAsync();

        _logger.LogInformation("Found {Count} eligible employees for mission assignment", eligibleEmployees.Count);

        ViewBag.Employees = eligibleEmployees;

        // Get all managers (employees with Line Manager role) for dropdown
        var managerEmployeeIds = await _context.Employee_Role
            .Include(er => er.role)
            .Where(er => er.role.role_name == AuthorizationHelper.LineManagerRole)
            .Select(er => er.employee_id)
            .Distinct()
            .ToListAsync();

        var eligibleManagers = await _context.Employee
            .Where(e => e.is_active == true && managerEmployeeIds.Contains(e.employee_id))
            .OrderBy(e => e.full_name)
            .ToListAsync();

        _logger.LogInformation("Found {Count} eligible managers", eligibleManagers.Count);

        ViewBag.Managers = eligibleManagers;

        var mission = new Mission
        {
            start_date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        return View(mission);
    }

    /// <summary>
    /// HR Admin: Create/assign a new mission
    /// </summary>
    [HttpPost]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Mission mission, int employeeId, int? managerId)
    {
        _logger.LogInformation("HR Admin attempting to create mission for employeeId={EmployeeId}, managerId={ManagerId}, destination={Destination}", 
            employeeId, managerId, mission.destination);

        // Re-populate dropdowns for validation errors
        // Get IDs of employees who have admin or manager roles
        var adminManagerEmployeeIds = await _context.Employee_Role
            .Include(er => er.role)
            .Where(er => er.role.role_name == AuthorizationHelper.SystemAdminRole ||
                        er.role.role_name == AuthorizationHelper.HRAdminRole ||
                        er.role.role_name == AuthorizationHelper.LineManagerRole)
            .Select(er => er.employee_id)
            .Distinct()
            .ToListAsync();

        // Get all active employees EXCLUDING admins and managers
        ViewBag.Employees = await _context.Employee
            .Where(e => e.is_active == true && !adminManagerEmployeeIds.Contains(e.employee_id))
            .OrderBy(e => e.full_name)
            .ToListAsync();

        // Get all managers (employees with Line Manager role) for dropdown
        var managerEmployeeIds = await _context.Employee_Role
            .Include(er => er.role)
            .Where(er => er.role.role_name == AuthorizationHelper.LineManagerRole)
            .Select(er => er.employee_id)
            .Distinct()
            .ToListAsync();

        ViewBag.Managers = await _context.Employee
            .Where(e => e.is_active == true && managerEmployeeIds.Contains(e.employee_id))
            .OrderBy(e => e.full_name)
            .ToListAsync();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(mission.destination))
        {
            ModelState.AddModelError(nameof(mission.destination), "Destination is required.");
        }

        if (!mission.start_date.HasValue)
        {
            ModelState.AddModelError(nameof(mission.start_date), "Start date is required.");
        }

        // Validate employee exists
        var employee = await _context.Employee
            .FirstOrDefaultAsync(e => e.employee_id == employeeId && e.is_active == true);
        if (employee == null)
        {
            ModelState.AddModelError(string.Empty, "Selected employee not found or is inactive.");
        }
        else
        {
            // Ensure the employee is not an admin or manager
            var employeeRoles = await AuthorizationHelper.GetEmployeeRolesAsync(_context, employeeId);
            if (employeeRoles.Contains(AuthorizationHelper.SystemAdminRole) ||
                employeeRoles.Contains(AuthorizationHelper.HRAdminRole) ||
                employeeRoles.Contains(AuthorizationHelper.LineManagerRole))
            {
                ModelState.AddModelError(string.Empty, "Missions can only be assigned to normal employees, not admins or managers.");
            }
        }

        // Validate manager exists if provided
        if (managerId.HasValue)
        {
            var manager = await _context.Employee
                .FirstOrDefaultAsync(e => e.employee_id == managerId.Value && e.is_active == true);
            if (manager == null)
            {
                ModelState.AddModelError(string.Empty, "Selected manager not found or is inactive.");
            }
            else
            {
                // Verify the employee has Line Manager role
                var isManager = await AuthorizationHelper.IsLineManagerAsync(_context, managerId.Value);
                if (!isManager)
                {
                    ModelState.AddModelError(string.Empty, "Selected manager does not have Line Manager role.");
                }
            }
        }

        // Validate end date is not before start date
        if (mission.start_date.HasValue && mission.end_date.HasValue)
        {
            if (mission.end_date.Value < mission.start_date.Value)
            {
                ModelState.AddModelError(nameof(mission.end_date), "End date cannot be before start date.");
            }
        }

        // Remove validation errors for navigation properties (we set IDs directly)
        // Entity Framework might try to validate required navigation properties
        ModelState.Remove("employee");
        ModelState.Remove("manager");

        // Log all validation errors
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validation failed! Errors:");
            foreach (var error in ModelState)
            {
                foreach (var err in error.Value.Errors)
                {
                    _logger.LogWarning("  - {Key}: {Error}", error.Key, err.ErrorMessage);
                }
            }
            return View(mission);
        }

        _logger.LogInformation("All validations passed, proceeding to create mission");

        try
        {
            // Set mission properties
            mission.employee_id = employeeId;
            mission.manager_id = managerId;
            mission.status = "Pending"; // Initial status for manager approval

            _logger.LogInformation("About to create mission: Employee={EmployeeId}, Manager={ManagerId}, Destination={Destination}, StartDate={StartDate}, EndDate={EndDate}, Status={Status}",
                mission.employee_id, mission.manager_id, mission.destination, mission.start_date, mission.end_date, mission.status);

            // Create the mission
            var createdMission = await _missionService.CreateAsync(mission);

            _logger.LogInformation("Mission {MissionId} created by HR Admin for employee {EmployeeId}", 
                createdMission.mission_id, employeeId);
            
            // Verify it was saved
            var verifyMission = await _context.Mission.FirstOrDefaultAsync(m => m.mission_id == createdMission.mission_id);
            if (verifyMission != null)
            {
                _logger.LogInformation("VERIFIED: Mission {MissionId} exists in database", createdMission.mission_id);
            }
            else
            {
                _logger.LogError("ERROR: Mission {MissionId} NOT FOUND in database after creation!", createdMission.mission_id);
            }
            
            TempData["SuccessMessage"] = $"Mission to {mission.destination} has been assigned to {employee.full_name} successfully.";
            
            return RedirectToAction(nameof(Create));
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error creating mission for employee {EmployeeId}", employeeId);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the mission. Please try again.");
            return View(mission);
        }
    }
}

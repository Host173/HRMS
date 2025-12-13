using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HRMS.Data;
using HRMS.Models;
using HRMS.Helpers;

namespace HRMS.Controllers;

[Authorize]
public class ShiftManagementController : Controller
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<ShiftManagementController> _logger;

    public ShiftManagementController(HrmsDbContext context, ILogger<ShiftManagementController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int? GetCurrentEmployeeId()
    {
        return AuthorizationHelper.GetCurrentEmployeeId(User);
    }

    private async Task<bool> IsSystemAdminAsync()
    {
        var employeeId = GetCurrentEmployeeId();
        if (!employeeId.HasValue) return false;
        return await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
    }

    private async Task<bool> IsHRAdminAsync()
    {
        var employeeId = GetCurrentEmployeeId();
        if (!employeeId.HasValue) return false;
        return await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);
    }

    private async Task<bool> IsManagerAsync()
    {
        var employeeId = GetCurrentEmployeeId();
        if (!employeeId.HasValue) return false;
        return await AuthorizationHelper.IsLineManagerAsync(_context, employeeId.Value) ||
               _context.Employee.Any(e => e.manager_id == employeeId.Value);
    }

    // Main Index - Shows different views based on role
    public async Task<IActionResult> Index()
    {
        var employeeId = GetCurrentEmployeeId();
        if (!employeeId.HasValue)
            return RedirectToAction("Login", "Account");

        var isSystemAdmin = await IsSystemAdminAsync();
        var isHRAdmin = await IsHRAdminAsync();
        var isManager = await IsManagerAsync();

        ViewBag.IsSystemAdmin = isSystemAdmin;
        ViewBag.IsHRAdmin = isHRAdmin;
        ViewBag.IsManager = isManager;

        // Get all shifts for display
        var shifts = await _context.ShiftSchedule
            .OrderBy(s => s.name)
            .ToListAsync();

        // Get all shift cycles (for rotational shifts)
        var cycles = await _context.ShiftCycle
            .Include(c => c.ShiftCycleAssignment)
                .ThenInclude(sca => sca.shift)
            .ToListAsync();

        // Get recent assignments
        var recentAssignments = await _context.ShiftAssignment
            .Include(sa => sa.employee)
            .Include(sa => sa.shift)
            .OrderByDescending(sa => sa.assigned_at)
            .Take(10)
            .ToListAsync();

        ViewBag.Shifts = shifts;
        ViewBag.Cycles = cycles;
        ViewBag.RecentAssignments = recentAssignments;

        return View();
    }

    // ========== System Admin: Create Shift Types ==========
    [HttpGet]
    public async Task<IActionResult> CreateShiftType()
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to create shift types.";
            return RedirectToAction("Index");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShiftType(ShiftSchedule shift)
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to create shift types.";
            return RedirectToAction("Index");
        }

        if (ModelState.IsValid)
        {
            shift.status = shift.status ?? "Active";
            _context.ShiftSchedule.Add(shift);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Shift type '{shift.name}' created successfully.";
            return RedirectToAction("Index");
        }

        return View(shift);
    }

    [HttpGet]
    public async Task<IActionResult> EditShiftType(int id)
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to edit shift types.";
            return RedirectToAction("Index");
        }

        var shift = await _context.ShiftSchedule.FindAsync(id);
        if (shift == null)
        {
            TempData["ErrorMessage"] = "Shift type not found.";
            return RedirectToAction("Index");
        }

        return View(shift);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditShiftType(int id, ShiftSchedule shift)
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to edit shift types.";
            return RedirectToAction("Index");
        }

        if (id != shift.shift_id)
        {
            TempData["ErrorMessage"] = "Invalid shift ID.";
            return RedirectToAction("Index");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(shift);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Shift type '{shift.name}' updated successfully.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ShiftSchedule.Any(s => s.shift_id == id))
                {
                    TempData["ErrorMessage"] = "Shift type not found.";
                    return RedirectToAction("Index");
                }
                throw;
            }
        }

        return View(shift);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShiftType(int id)
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to delete shift types.";
            return RedirectToAction("Index");
        }

        var shift = await _context.ShiftSchedule.FindAsync(id);
        if (shift == null)
        {
            TempData["ErrorMessage"] = "Shift type not found.";
            return RedirectToAction("Index");
        }

        // Check if shift is assigned to any employees
        var hasAssignments = await _context.ShiftAssignment.AnyAsync(sa => sa.shift_id == id);
        if (hasAssignments)
        {
            TempData["ErrorMessage"] = $"Cannot delete shift '{shift.name}' because it is assigned to employees. Please remove all assignments first.";
            return RedirectToAction("Index");
        }

        _context.ShiftSchedule.Remove(shift);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Shift type '{shift.name}' deleted successfully.";
        return RedirectToAction("Index");
    }

    // ========== HR Admin: Configure Split and Rotational Shifts ==========
    [HttpGet]
    public async Task<IActionResult> CreateRotationalShift()
    {
        if (!await IsHRAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to configure rotational shifts.";
            return RedirectToAction("Index");
        }

        var shifts = await _context.ShiftSchedule
            .Where(s => s.status == "Active" || s.status == null)
            .OrderBy(s => s.name)
            .ToListAsync();

        ViewBag.Shifts = shifts;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRotationalShift(string cycleName, string? description, List<int> shiftIds, List<int>? orderNumbers)
    {
        if (!await IsHRAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to configure rotational shifts.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(cycleName))
        {
            TempData["ErrorMessage"] = "Cycle name is required.";
            return RedirectToAction("CreateRotationalShift");
        }

        if (shiftIds == null || !shiftIds.Any())
        {
            TempData["ErrorMessage"] = "Please select at least one shift for the rotational cycle.";
            return RedirectToAction("CreateRotationalShift");
        }

        // Create the cycle
        var cycle = new ShiftCycle
        {
            cycle_name = cycleName,
            description = description
        };

        _context.ShiftCycle.Add(cycle);
        await _context.SaveChangesAsync();

        // Add shifts to the cycle
        for (int i = 0; i < shiftIds.Count; i++)
        {
            var cycleAssignment = new ShiftCycleAssignment
            {
                cycle_id = cycle.cycle_id,
                shift_id = shiftIds[i],
                order_number = orderNumbers != null && i < orderNumbers.Count ? orderNumbers[i] : i + 1
            };
            _context.ShiftCycleAssignment.Add(cycleAssignment);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Rotational shift cycle '{cycleName}' created successfully with {shiftIds.Count} shifts.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> EditRotationalShift(int id)
    {
        if (!await IsHRAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to edit rotational shifts.";
            return RedirectToAction("Index");
        }

        var cycle = await _context.ShiftCycle
            .Include(c => c.ShiftCycleAssignment)
                .ThenInclude(sca => sca.shift)
            .FirstOrDefaultAsync(c => c.cycle_id == id);

        if (cycle == null)
        {
            TempData["ErrorMessage"] = "Rotational shift cycle not found.";
            return RedirectToAction("Index");
        }

        var allShifts = await _context.ShiftSchedule
            .Where(s => s.status == "Active" || s.status == null)
            .OrderBy(s => s.name)
            .ToListAsync();

        ViewBag.AllShifts = allShifts;
        return View(cycle);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRotationalShift(int id, string cycleName, string? description, List<int>? shiftIds, List<int>? orderNumbers)
    {
        if (!await IsHRAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to edit rotational shifts.";
            return RedirectToAction("Index");
        }

        var cycle = await _context.ShiftCycle.FindAsync(id);
        if (cycle == null)
        {
            TempData["ErrorMessage"] = "Rotational shift cycle not found.";
            return RedirectToAction("Index");
        }

        cycle.cycle_name = cycleName;
        cycle.description = description;

        // Remove existing assignments
        var existingAssignments = await _context.ShiftCycleAssignment
            .Where(sca => sca.cycle_id == id)
            .ToListAsync();
        _context.ShiftCycleAssignment.RemoveRange(existingAssignments);

        // Add new assignments
        if (shiftIds != null && shiftIds.Any())
        {
            for (int i = 0; i < shiftIds.Count; i++)
            {
                var cycleAssignment = new ShiftCycleAssignment
                {
                    cycle_id = id,
                    shift_id = shiftIds[i],
                    order_number = orderNumbers != null && i < orderNumbers.Count ? orderNumbers[i] : i + 1
                };
                _context.ShiftCycleAssignment.Add(cycleAssignment);
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Rotational shift cycle '{cycleName}' updated successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRotationalShift(int id)
    {
        if (!await IsHRAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to delete rotational shifts.";
            return RedirectToAction("Index");
        }

        var cycle = await _context.ShiftCycle.FindAsync(id);
        if (cycle == null)
        {
            TempData["ErrorMessage"] = "Rotational shift cycle not found.";
            return RedirectToAction("Index");
        }

        // Remove all cycle assignments
        var assignments = await _context.ShiftCycleAssignment
            .Where(sca => sca.cycle_id == id)
            .ToListAsync();
        _context.ShiftCycleAssignment.RemoveRange(assignments);

        _context.ShiftCycle.Remove(cycle);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Rotational shift cycle '{cycle.cycle_name}' deleted successfully.";
        return RedirectToAction("Index");
    }

    // ========== System Admin & Manager: Assign Shifts to Employees or Departments ==========
    [HttpGet]
    public async Task<IActionResult> AssignShift()
    {
        var isSystemAdmin = await IsSystemAdminAsync();
        var isManager = await IsManagerAsync();

        if (!isSystemAdmin && !isManager)
        {
            TempData["ErrorMessage"] = "You don't have permission to assign shifts.";
            return RedirectToAction("Index");
        }

        var employeeId = GetCurrentEmployeeId();
        
        // Load all shifts (not just Active ones) so users can see all available shifts
        var shifts = await _context.ShiftSchedule
            .OrderBy(s => s.name)
            .ToListAsync();

        List<Employee> employees;
        List<Department> departments;

        if (isSystemAdmin)
        {
            // System Admin can see all employees and departments
            employees = await _context.Employee
                .Where(e => e.is_active == true || e.is_active == null)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            departments = await _context.Department
                .OrderBy(d => d.department_name)
                .ToListAsync();
        }
        else if (employeeId.HasValue)
        {
            // Manager can only see their team
            employees = await _context.Employee
                .Where(e => e.manager_id == employeeId.Value && (e.is_active == true || e.is_active == null))
                .OrderBy(e => e.full_name)
                .ToListAsync();
            departments = new List<Department>(); // Managers can't assign to departments
        }
        else
        {
            employees = new List<Employee>();
            departments = new List<Department>();
        }

        ViewBag.Shifts = shifts;
        ViewBag.Employees = employees;
        ViewBag.Departments = departments;
        ViewBag.IsSystemAdmin = isSystemAdmin;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignShift(int shiftId, string assignTo, int? employeeId, int? departmentId, DateOnly? startDate, DateOnly? endDate, string? notes)
    {
        var isSystemAdmin = await IsSystemAdminAsync();
        var isManager = await IsManagerAsync();

        if (!isSystemAdmin && !isManager)
        {
            TempData["ErrorMessage"] = "You don't have permission to assign shifts.";
            return RedirectToAction("Index");
        }

        var currentEmployeeId = GetCurrentEmployeeId();
        var shift = await _context.ShiftSchedule.FindAsync(shiftId);
        if (shift == null)
        {
            TempData["ErrorMessage"] = "Shift not found.";
            return RedirectToAction("AssignShift");
        }

        List<Employee> targetEmployees = new List<Employee>();

        if (assignTo == "employee" && employeeId.HasValue)
        {
            var employee = await _context.Employee.FindAsync(employeeId.Value);
            if (employee != null)
            {
                // Check if manager is trying to assign to their team member
                if (!isSystemAdmin && employee.manager_id != currentEmployeeId)
                {
                    TempData["ErrorMessage"] = "You can only assign shifts to your team members.";
                    return RedirectToAction("AssignShift");
                }
                targetEmployees.Add(employee);
            }
        }
        else if (assignTo == "department" && departmentId.HasValue && isSystemAdmin)
        {
            targetEmployees = await _context.Employee
                .Where(e => e.department_id == departmentId && (e.is_active == true || e.is_active == null))
                .ToListAsync();
        }
        else
        {
            TempData["ErrorMessage"] = "Invalid assignment parameters.";
            return RedirectToAction("AssignShift");
        }

        if (!targetEmployees.Any())
        {
            TempData["ErrorMessage"] = "No employees found for assignment.";
            return RedirectToAction("AssignShift");
        }

        // Create assignments
        foreach (var employee in targetEmployees)
        {
            var assignment = new ShiftAssignment
            {
                employee_id = employee.employee_id,
                shift_id = shiftId,
                start_date = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                end_date = endDate,
                status = "Active",
                assigned_at = DateTime.UtcNow,
                notes = notes
            };
            _context.ShiftAssignment.Add(assignment);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Shift '{shift.name}' assigned to {targetEmployees.Count} employee(s) successfully.";
        return RedirectToAction("Index");
    }

    // ========== System Admin: Manage Individual Employee Shifts ==========
    [HttpGet]
    public async Task<IActionResult> ManageEmployeeShifts(int? employeeId)
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to manage employee shifts.";
            return RedirectToAction("Index");
        }

        var employees = await _context.Employee
            .Where(e => e.is_active == true || e.is_active == null)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        var shifts = await _context.ShiftSchedule
            .Where(s => s.status == "Active" || s.status == null)
            .OrderBy(s => s.name)
            .ToListAsync();

        ViewBag.Employees = employees;
        ViewBag.Shifts = shifts;

        if (employeeId.HasValue)
        {
            var assignments = await _context.ShiftAssignment
                .Include(sa => sa.shift)
                .Include(sa => sa.employee)
                .Where(sa => sa.employee_id == employeeId.Value)
                .OrderByDescending(sa => sa.start_date)
                .ToListAsync();

            var employee = await _context.Employee.FindAsync(employeeId.Value);
            ViewBag.SelectedEmployee = employee;
            ViewBag.Assignments = assignments;
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEmployeeShift(int employeeId, int shiftId, DateOnly? startDate, DateOnly? endDate, string? notes)
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to assign shifts.";
            return RedirectToAction("Index");
        }

        var assignment = new ShiftAssignment
        {
            employee_id = employeeId,
            shift_id = shiftId,
            start_date = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            end_date = endDate,
            status = "Active",
            assigned_at = DateTime.UtcNow,
            notes = notes
        };

        _context.ShiftAssignment.Add(assignment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Shift assigned successfully.";
        return RedirectToAction("ManageEmployeeShifts", new { employeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmployeeShift(int assignmentId, int shiftId, DateOnly? startDate, DateOnly? endDate, string? notes, string? status)
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to update shifts.";
            return RedirectToAction("Index");
        }

        var assignment = await _context.ShiftAssignment.FindAsync(assignmentId);
        if (assignment == null)
        {
            TempData["ErrorMessage"] = "Assignment not found.";
            return RedirectToAction("Index");
        }

        assignment.shift_id = shiftId;
        assignment.start_date = startDate ?? assignment.start_date;
        assignment.end_date = endDate;
        assignment.notes = notes;
        assignment.status = status ?? assignment.status;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Shift assignment updated successfully.";
        return RedirectToAction("ManageEmployeeShifts", new { employeeId = assignment.employee_id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmployeeShift(int assignmentId)
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to delete shift assignments.";
            return RedirectToAction("Index");
        }

        var assignment = await _context.ShiftAssignment.FindAsync(assignmentId);
        if (assignment == null)
        {
            TempData["ErrorMessage"] = "Assignment not found.";
            return RedirectToAction("Index");
        }

        var employeeId = assignment.employee_id;
        _context.ShiftAssignment.Remove(assignment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Shift assignment deleted successfully.";
        return RedirectToAction("ManageEmployeeShifts", new { employeeId });
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;

namespace HRMS.Controllers;

[Authorize]
[RequireRole(AuthorizationHelper.HRAdminRole)]
public class LeaveEntitlementController : Controller
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<LeaveEntitlementController> _logger;

    public LeaveEntitlementController(HrmsDbContext context, ILogger<LeaveEntitlementController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? employeeId, int? leaveTypeId)
    {
        var query = _context.LeaveEntitlement
            .Include(le => le.employee)
            .Include(le => le.leave_type)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(le => le.employee_id == employeeId.Value);
        }

        if (leaveTypeId.HasValue)
        {
            query = query.Where(le => le.leave_type_id == leaveTypeId.Value);
        }

        var entitlements = await query
            .OrderBy(le => le.employee.full_name)
            .ThenBy(le => le.leave_type.leave_type)
            .ToListAsync();

        ViewBag.Employees = await _context.Employee
            .Where(e => e.is_active == true)
            .OrderBy(e => e.full_name)
            .ToListAsync();
        ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();

        return View(entitlements);
    }

    [HttpGet]
    public async Task<IActionResult> Adjust(int? employeeId, int? leaveTypeId)
    {
        var model = new LeaveEntitlementAdjustmentViewModel();

        if (employeeId.HasValue)
        {
            var employee = await _context.Employee.FindAsync(employeeId.Value);
            if (employee != null)
            {
                model.EmployeeId = employeeId.Value;
                model.EmployeeName = employee.full_name ?? string.Empty;
                model.EmployeeEmail = employee.email ?? string.Empty;

                if (leaveTypeId.HasValue)
                {
                    var leaveType = await _context.Leave.FindAsync(leaveTypeId.Value);
                    if (leaveType != null)
                    {
                        model.LeaveTypeId = leaveTypeId.Value;
                        model.LeaveTypeName = leaveType.leave_type;

                        // Get existing entitlement
                        var existing = await _context.LeaveEntitlement
                            .FirstOrDefaultAsync(le => le.employee_id == employeeId.Value && le.leave_type_id == leaveTypeId.Value);
                        if (existing != null)
                        {
                            model.Entitlement = existing.entitlement ?? 0;
                        }
                    }
                }
            }
        }

        ViewBag.Employees = await _context.Employee
            .Where(e => e.is_active == true)
            .OrderBy(e => e.full_name)
            .ToListAsync();
        ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(LeaveEntitlementAdjustmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
            return View(model);
        }

        try
        {
            var existing = await _context.LeaveEntitlement
                .FirstOrDefaultAsync(le => le.employee_id == model.EmployeeId && le.leave_type_id == model.LeaveTypeId);

            if (existing != null)
            {
                existing.entitlement = model.Entitlement;
            }
            else
            {
                // Use existing stored procedure for initial entitlement assignment
                var leaveType = await _context.Leave.FindAsync(model.LeaveTypeId);
                if (leaveType == null)
                {
                    ModelState.AddModelError(nameof(model.LeaveTypeId), "Selected leave type is not valid.");
                    ViewBag.Employees = await _context.Employee
                        .Where(e => e.is_active == true)
                        .OrderBy(e => e.full_name)
                        .ToListAsync();
                    ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
                    return View(model);
                }

                var employeeIdParam = new SqlParameter("@EmployeeID", model.EmployeeId);
                var leaveTypeParam = new SqlParameter("@LeaveType", leaveType.leave_type);
                var entitlementParam = new SqlParameter("@Entitlement", model.Entitlement);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC AssignLeaveEntitlement @EmployeeID, @LeaveType, @Entitlement",
                    employeeIdParam,
                    leaveTypeParam,
                    entitlementParam);
            }

            await _context.SaveChangesAsync();

            var employee = await _context.Employee.FindAsync(model.EmployeeId);
            var leaveTypeForMessage = await _context.Leave.FindAsync(model.LeaveTypeId);

            _logger.LogInformation("HR Admin adjusted leave entitlement for employee {EmployeeId}, leave type {LeaveTypeId}, new entitlement: {Entitlement}",
                model.EmployeeId, model.LeaveTypeId, model.Entitlement);
            TempData["SuccessMessage"] = $"Leave entitlement for {employee?.full_name} - {leaveTypeForMessage?.leave_type} adjusted to {model.Entitlement} days.";
            return RedirectToAction("Index", new { employeeId = model.EmployeeId });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error adjusting leave entitlement");
            ModelState.AddModelError(string.Empty, "An error occurred while adjusting the leave entitlement. Please try again.");
            ViewBag.Employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
            return View(model);
        }
    }
}






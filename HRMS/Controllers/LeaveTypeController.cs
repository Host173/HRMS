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
public class LeaveTypeController : Controller
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<LeaveTypeController> _logger;

    public LeaveTypeController(HrmsDbContext context, ILogger<LeaveTypeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var leaveTypes = await _context.Leave
            .Select(l => new LeaveTypeViewModel
            {
                LeaveId = l.leave_id,
                LeaveType = l.leave_type,
                LeaveDescription = l.leave_description,
                IsActive = true, // You may want to add an is_active field to Leave table
                IsSpecialLeave = l.leave_type.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                                l.leave_type.Contains("Holiday", StringComparison.OrdinalIgnoreCase),
                RequestCount = l.LeaveRequest.Count
            })
            .OrderBy(l => l.LeaveType)
            .ToListAsync();

        return View(leaveTypes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new LeaveTypeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Use existing stored procedure (DB schema/procs must be reused as-is)
            var leaveTypeParam = new SqlParameter("@LeaveType", model.LeaveType);
            var descriptionParam = new SqlParameter("@Description", (object?)model.LeaveDescription ?? DBNull.Value);
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC ManageLeaveTypes @LeaveType, @Description",
                leaveTypeParam,
                descriptionParam);

            _logger.LogInformation("HR Admin created new leave type: {LeaveType}", model.LeaveType);
            TempData["SuccessMessage"] = $"Leave type '{model.LeaveType}' created successfully!";
            return RedirectToAction("Index");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error creating leave type: {LeaveType}", model.LeaveType);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the leave type. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var leave = await _context.Leave.FindAsync(id);
        if (leave == null)
        {
            return NotFound();
        }

        var model = new LeaveTypeViewModel
        {
            LeaveId = leave.leave_id,
            LeaveType = leave.leave_type,
            LeaveDescription = leave.leave_description,
            IsSpecialLeave = leave.leave_type.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                           leave.leave_type.Contains("Holiday", StringComparison.OrdinalIgnoreCase)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeaveTypeViewModel model)
    {
        if (id != model.LeaveId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var leave = await _context.Leave.FindAsync(id);
            if (leave == null)
            {
                return NotFound();
            }

            leave.leave_type = model.LeaveType;
            leave.leave_description = model.LeaveDescription;

            await _context.SaveChangesAsync();

            _logger.LogInformation("HR Admin updated leave type: {LeaveType}", model.LeaveType);
            TempData["SuccessMessage"] = $"Leave type '{model.LeaveType}' updated successfully!";
            return RedirectToAction("Index");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error updating leave type: {LeaveType}", model.LeaveType);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the leave type. Please try again.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var leave = await _context.Leave
                .Include(l => l.LeaveRequest)
                .FirstOrDefaultAsync(l => l.leave_id == id);

            if (leave == null)
            {
                return NotFound();
            }

            // Check if there are any leave requests using this type
            if (leave.LeaveRequest.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete leave type '{leave.leave_type}' because it has {leave.LeaveRequest.Count} associated leave request(s).";
                return RedirectToAction("Index");
            }

            _context.Leave.Remove(leave);
            await _context.SaveChangesAsync();

            _logger.LogInformation("HR Admin deleted leave type: {LeaveType}", leave.leave_type);
            TempData["SuccessMessage"] = $"Leave type '{leave.leave_type}' deleted successfully!";
            return RedirectToAction("Index");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave type: {LeaveTypeId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the leave type. Please try again.";
            return RedirectToAction("Index");
        }
    }
}







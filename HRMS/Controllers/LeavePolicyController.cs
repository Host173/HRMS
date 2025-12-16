using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;

namespace HRMS.Controllers;

[Authorize]
[RequireRole(AuthorizationHelper.HRAdminRole)]
public class LeavePolicyController : Controller
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<LeavePolicyController> _logger;

    public LeavePolicyController(HrmsDbContext context, ILogger<LeavePolicyController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? leaveTypeId)
    {
        // Ensure DbSet is available
        if (_context.LeavePolicy == null)
        {
            ViewBag.LeaveTypes = new List<Leave>();
            return View(new List<LeavePolicyViewModel>());
        }

        // Query only existing columns to avoid "Invalid column name" errors
        // NOTE: Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to add new columns
        var policies = await _context.LeavePolicy
            .Select(p => new LeavePolicyViewModel
            {
                PolicyId = p.policy_id,
                Name = p.name,
                LeaveTypeId = 0, // Column doesn't exist yet - set default
                LeaveTypeName = "N/A", // Navigation not available without leave_type_id
                Purpose = p.purpose,
                EligibilityRules = p.eligibility_rules,
                DocumentationRequirements = null, // Column doesn't exist yet
                ApprovalWorkflow = null, // Column doesn't exist yet
                NoticePeriod = p.notice_period,
                SpecialLeaveType = p.special_leave_type,
                ResetOnNewYear = p.reset_on_new_year == true,
                IsActive = true, // Column doesn't exist yet - default to active
                RequiresHRAdminApproval = false, // Column doesn't exist yet - default to false
                MaxDaysPerRequest = null, // Column doesn't exist yet
                MinDaysPerRequest = null, // Column doesn't exist yet
                RequiresDocumentation = false // Column doesn't exist yet - default to false
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        // Filter by leaveTypeId in memory if needed (since leave_type_id column doesn't exist)
        if (leaveTypeId.HasValue)
        {
            // Note: Cannot filter by leave_type_id since column doesn't exist
            // All policies will be shown until SQL_ADD_LEAVE_POLICY_COLUMNS.sql is run
            ViewBag.LeaveType = await _context.Leave.FindAsync(leaveTypeId.Value);
        }

        // Safe query for LeaveTypes - handle empty table gracefully
        ViewBag.LeaveTypes = _context.Leave != null 
            ? await _context.Leave.OrderBy(l => l.leave_type).ToListAsync() 
            : new List<Leave>();
        
        return View(policies ?? new List<LeavePolicyViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? leaveTypeId)
    {
        var model = new LeavePolicyViewModel
        {
            IsActive = true,
            RequiresDocumentation = false,
            ResetOnNewYear = false
        };

        if (leaveTypeId.HasValue)
        {
            var leaveType = await _context.Leave.FindAsync(leaveTypeId.Value);
            if (leaveType != null)
            {
                model.LeaveTypeId = leaveTypeId.Value;
                model.LeaveTypeName = leaveType.leave_type;
                model.RequiresHRAdminApproval = leaveType.leave_type.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                                              leaveType.leave_type.Contains("Holiday", StringComparison.OrdinalIgnoreCase);
            }
        }

        ViewBag.LeaveTypes = _context.Leave != null 
            ? await _context.Leave.OrderBy(l => l.leave_type).ToListAsync() 
            : new List<Leave>();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeavePolicyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.LeaveTypes = _context.Leave != null 
                ? await _context.Leave.OrderBy(l => l.leave_type).ToListAsync() 
                : new List<Leave>();
            return View(model);
        }

        try
        {
            // NOTE: Only set columns that exist in database
            // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable new columns
            var policy = new LeavePolicy
            {
                name = model.Name,
                // leave_type_id = model.LeaveTypeId, // Column doesn't exist yet
                purpose = model.Purpose,
                eligibility_rules = model.EligibilityRules,
                // documentation_requirements = model.DocumentationRequirements, // Column doesn't exist yet
                // approval_workflow = model.ApprovalWorkflow, // Column doesn't exist yet
                notice_period = model.NoticePeriod,
                special_leave_type = model.SpecialLeaveType,
                reset_on_new_year = model.ResetOnNewYear ? true : (bool?)null
                // is_active = model.IsActive, // Column doesn't exist yet
                // requires_hr_admin_approval = model.RequiresHRAdminApproval, // Column doesn't exist yet
                // max_days_per_request = model.MaxDaysPerRequest, // Column doesn't exist yet
                // min_days_per_request = model.MinDaysPerRequest, // Column doesn't exist yet
                // requires_documentation = model.RequiresDocumentation // Column doesn't exist yet
            };

            _context.LeavePolicy.Add(policy);
            await _context.SaveChangesAsync();

            _logger.LogInformation("HR Admin created leave policy: {PolicyName} for leave type {LeaveTypeId}", 
                model.Name, model.LeaveTypeId);
            TempData["SuccessMessage"] = $"Leave policy '{model.Name}' created successfully!";
            return RedirectToAction("Index", new { leaveTypeId = model.LeaveTypeId });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error creating leave policy: {PolicyName}", model.Name);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the leave policy. Please try again.");
            ViewBag.LeaveTypes = _context.Leave != null 
                ? await _context.Leave.OrderBy(l => l.leave_type).ToListAsync() 
                : new List<Leave>();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // NOTE: Cannot use Include(leave_type) or read new columns that don't exist yet
        // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable full functionality
        var policy = await _context.LeavePolicy
            .FirstOrDefaultAsync(p => p.policy_id == id);

        if (policy == null)
        {
            return NotFound();
        }

        var model = new LeavePolicyViewModel
        {
            PolicyId = policy.policy_id,
            Name = policy.name,
            LeaveTypeId = 0, // Column doesn't exist yet
            LeaveTypeName = "N/A", // Navigation not available
            Purpose = policy.purpose,
            EligibilityRules = policy.eligibility_rules,
            DocumentationRequirements = null, // Column doesn't exist yet
            ApprovalWorkflow = null, // Column doesn't exist yet
            NoticePeriod = policy.notice_period,
            SpecialLeaveType = policy.special_leave_type,
            ResetOnNewYear = policy.reset_on_new_year == true,
            IsActive = true, // Column doesn't exist yet - default
            RequiresHRAdminApproval = false, // Column doesn't exist yet - default
            MaxDaysPerRequest = null, // Column doesn't exist yet
            MinDaysPerRequest = null, // Column doesn't exist yet
            RequiresDocumentation = false // Column doesn't exist yet - default
        };

        ViewBag.LeaveTypes = _context.Leave != null 
            ? await _context.Leave.OrderBy(l => l.leave_type).ToListAsync() 
            : new List<Leave>();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeavePolicyViewModel model)
    {
        if (id != model.PolicyId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.LeaveTypes = _context.Leave != null 
                ? await _context.Leave.OrderBy(l => l.leave_type).ToListAsync() 
                : new List<Leave>();
            return View(model);
        }

        try
        {
            var policy = await _context.LeavePolicy.FindAsync(id);
            if (policy == null)
            {
                return NotFound();
            }

            // NOTE: Only set columns that exist in database
            // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable new columns
            policy.name = model.Name;
            // policy.leave_type_id = model.LeaveTypeId; // Column doesn't exist yet
            policy.purpose = model.Purpose;
            policy.eligibility_rules = model.EligibilityRules;
            // policy.documentation_requirements = model.DocumentationRequirements; // Column doesn't exist yet
            // policy.approval_workflow = model.ApprovalWorkflow; // Column doesn't exist yet
            policy.notice_period = model.NoticePeriod;
            policy.special_leave_type = model.SpecialLeaveType;
            policy.reset_on_new_year = model.ResetOnNewYear ? true : (bool?)null;
            // policy.is_active = model.IsActive; // Column doesn't exist yet
            // policy.requires_hr_admin_approval = model.RequiresHRAdminApproval; // Column doesn't exist yet
            // policy.max_days_per_request = model.MaxDaysPerRequest; // Column doesn't exist yet
            // policy.min_days_per_request = model.MinDaysPerRequest; // Column doesn't exist yet
            // policy.requires_documentation = model.RequiresDocumentation; // Column doesn't exist yet

            await _context.SaveChangesAsync();

            _logger.LogInformation("HR Admin updated leave policy: {PolicyName}", model.Name);
            TempData["SuccessMessage"] = $"Leave policy '{model.Name}' updated successfully!";
            return RedirectToAction("Index", new { leaveTypeId = model.LeaveTypeId });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error updating leave policy: {PolicyName}", model.Name);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the leave policy. Please try again.");
            ViewBag.LeaveTypes = _context.Leave != null 
                ? await _context.Leave.OrderBy(l => l.leave_type).ToListAsync() 
                : new List<Leave>();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        try
        {
            var policy = await _context.LeavePolicy.FindAsync(id);
            if (policy == null)
            {
                return NotFound();
            }

            // NOTE: is_active column doesn't exist yet - cannot toggle
            // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable this functionality
            TempData["ErrorMessage"] = "Toggle active functionality requires database columns that don't exist yet. Please run SQL_ADD_LEAVE_POLICY_COLUMNS.sql";
            return RedirectToAction("Index");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error toggling leave policy status: {PolicyId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the policy status.";
            return RedirectToAction("Index");
        }
    }
}



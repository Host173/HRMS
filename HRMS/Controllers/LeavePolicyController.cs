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
        var query = _context.LeavePolicy.AsQueryable();

        if (leaveTypeId.HasValue)
        {
            query = query.Where(p => p.leave_type_id == leaveTypeId.Value);
            ViewBag.LeaveType = await _context.Leave.FindAsync(leaveTypeId.Value);
        }

        var policies = await query
            .Include(p => p.leave_type)
            .Select(p => new LeavePolicyViewModel
            {
                PolicyId = p.policy_id,
                Name = p.name,
                LeaveTypeId = p.leave_type_id ?? 0,
                LeaveTypeName = p.leave_type != null ? p.leave_type.leave_type : "N/A",
                Purpose = p.purpose,
                EligibilityRules = p.eligibility_rules,
                DocumentationRequirements = p.documentation_requirements,
                ApprovalWorkflow = p.approval_workflow,
                NoticePeriod = p.notice_period,
                SpecialLeaveType = p.special_leave_type,
                ResetOnNewYear = p.reset_on_new_year == true,
                IsActive = p.is_active ?? true,
                RequiresHRAdminApproval = p.requires_hr_admin_approval ?? false,
                MaxDaysPerRequest = p.max_days_per_request,
                MinDaysPerRequest = p.min_days_per_request,
                RequiresDocumentation = p.requires_documentation ?? false
            })
            .OrderBy(p => p.LeaveTypeName)
            .ThenBy(p => p.Name)
            .ToListAsync();

        ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
        return View(policies);
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

        ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeavePolicyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
            return View(model);
        }

        try
        {
            var policy = new LeavePolicy
            {
                name = model.Name,
                leave_type_id = model.LeaveTypeId,
                purpose = model.Purpose,
                eligibility_rules = model.EligibilityRules,
                documentation_requirements = model.DocumentationRequirements,
                approval_workflow = model.ApprovalWorkflow,
                notice_period = model.NoticePeriod,
                special_leave_type = model.SpecialLeaveType,
                reset_on_new_year = model.ResetOnNewYear ? true : (bool?)null,
                is_active = model.IsActive,
                requires_hr_admin_approval = model.RequiresHRAdminApproval,
                max_days_per_request = model.MaxDaysPerRequest,
                min_days_per_request = model.MinDaysPerRequest,
                requires_documentation = model.RequiresDocumentation
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
            ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var policy = await _context.LeavePolicy
            .Include(p => p.leave_type)
            .FirstOrDefaultAsync(p => p.policy_id == id);

        if (policy == null)
        {
            return NotFound();
        }

        var model = new LeavePolicyViewModel
        {
            PolicyId = policy.policy_id,
            Name = policy.name,
            LeaveTypeId = policy.leave_type_id ?? 0,
            LeaveTypeName = policy.leave_type?.leave_type ?? "N/A",
            Purpose = policy.purpose,
            EligibilityRules = policy.eligibility_rules,
            DocumentationRequirements = policy.documentation_requirements,
            ApprovalWorkflow = policy.approval_workflow,
            NoticePeriod = policy.notice_period,
            SpecialLeaveType = policy.special_leave_type,
            ResetOnNewYear = policy.reset_on_new_year == true,
            IsActive = policy.is_active ?? true,
            RequiresHRAdminApproval = policy.requires_hr_admin_approval ?? false,
            MaxDaysPerRequest = policy.max_days_per_request,
            MinDaysPerRequest = policy.min_days_per_request,
            RequiresDocumentation = policy.requires_documentation ?? false
        };

        ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
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
            ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
            return View(model);
        }

        try
        {
            var policy = await _context.LeavePolicy.FindAsync(id);
            if (policy == null)
            {
                return NotFound();
            }

            policy.name = model.Name;
            policy.leave_type_id = model.LeaveTypeId;
            policy.purpose = model.Purpose;
            policy.eligibility_rules = model.EligibilityRules;
            policy.documentation_requirements = model.DocumentationRequirements;
            policy.approval_workflow = model.ApprovalWorkflow;
            policy.notice_period = model.NoticePeriod;
            policy.special_leave_type = model.SpecialLeaveType;
            policy.reset_on_new_year = model.ResetOnNewYear ? true : (bool?)null;
            policy.is_active = model.IsActive;
            policy.requires_hr_admin_approval = model.RequiresHRAdminApproval;
            policy.max_days_per_request = model.MaxDaysPerRequest;
            policy.min_days_per_request = model.MinDaysPerRequest;
            policy.requires_documentation = model.RequiresDocumentation;

            await _context.SaveChangesAsync();

            _logger.LogInformation("HR Admin updated leave policy: {PolicyName}", model.Name);
            TempData["SuccessMessage"] = $"Leave policy '{model.Name}' updated successfully!";
            return RedirectToAction("Index", new { leaveTypeId = model.LeaveTypeId });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error updating leave policy: {PolicyName}", model.Name);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the leave policy. Please try again.");
            ViewBag.LeaveTypes = await _context.Leave.OrderBy(l => l.leave_type).ToListAsync();
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

            policy.is_active = !(policy.is_active ?? true);
            await _context.SaveChangesAsync();

            var status = policy.is_active == true ? "activated" : "deactivated";
            _logger.LogInformation("HR Admin {Status} leave policy: {PolicyId}", status, id);
            TempData["SuccessMessage"] = $"Leave policy '{policy.name}' {status} successfully!";
            return RedirectToAction("Index", new { leaveTypeId = policy.leave_type_id });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error toggling leave policy status: {PolicyId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the policy status.";
            return RedirectToAction("Index");
        }
    }
}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.Services;
using System.IO;

namespace HRMS.Controllers;

[Authorize]
[RequireRole(AuthorizationHelper.HRAdminRole)]
public class LeaveOverrideController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<LeaveOverrideController> _logger;

    public LeaveOverrideController(
        ILeaveService leaveService,
        HrmsDbContext context,
        ILogger<LeaveOverrideController> logger)
    {
        _leaveService = leaveService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Get all leave policies that require HR Admin approval (special leave)
        var specialLeavePolicies = await _context.LeavePolicy
            .Where(p => p.requires_hr_admin_approval == true && (p.is_active ?? true) && p.leave_type_id.HasValue)
            .Select(p => p.leave_type_id!.Value)
            .ToListAsync();

        // Get all leave requests EXCLUDING special leave requests (those are managed separately)
        var requests = await _context.LeaveRequest
            .Include(lr => lr.employee)
            .Include(lr => lr.leave)
            .Include(lr => lr.LeaveDocument)
            .Where(lr => !specialLeavePolicies.Contains(lr.leave_id))
            .OrderByDescending(lr => lr.request_id)
            .ToListAsync();

        var viewModels = requests.Select(r => new LeaveRequestViewModel
        {
            RequestId = r.request_id,
            EmployeeId = r.employee_id,
            EmployeeName = r.employee?.full_name ?? string.Empty,
            EmployeeEmail = r.employee?.email ?? string.Empty,
            LeaveType = r.leave?.leave_type ?? string.Empty,
            StartDate = r.start_date ?? DateOnly.FromDateTime(DateTime.Today),
            EndDate = r.end_date ?? DateOnly.FromDateTime(DateTime.Today),
            Duration = r.duration,
            Justification = r.justification,
            Status = r.status,
            ApprovedBy = r.approved_by,
            IsIrregular = r.is_irregular,
            IrregularityReason = r.irregularity_reason,
            CreatedAt = r.created_at,
            Documents = r.LeaveDocument?.Select(d => new LeaveDocumentViewModel
            {
                DocumentId = d.document_id,
                FileName = Path.GetFileName(d.file_path ?? string.Empty),
                FilePath = d.file_path ?? string.Empty,
                UploadedAt = d.uploaded_at
            }).ToList() ?? new List<LeaveDocumentViewModel>()
        }).ToList();

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var request = await _leaveService.GetByIdAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        var viewModel = new LeaveRequestViewModel
        {
            RequestId = request.request_id,
            EmployeeId = request.employee_id,
            EmployeeName = request.employee?.full_name ?? string.Empty,
            EmployeeEmail = request.employee?.email ?? string.Empty,
            LeaveType = request.leave?.leave_type ?? string.Empty,
            StartDate = request.start_date ?? DateOnly.FromDateTime(DateTime.Today),
            EndDate = request.end_date ?? DateOnly.FromDateTime(DateTime.Today),
            Duration = request.duration,
            Justification = request.justification,
            Status = request.status,
            ApprovedBy = request.approved_by,
            IsIrregular = request.is_irregular,
            IrregularityReason = request.irregularity_reason,
            CreatedAt = request.created_at,
            Documents = request.LeaveDocument?.Select(d => new LeaveDocumentViewModel
            {
                DocumentId = d.document_id,
                FileName = Path.GetFileName(d.file_path ?? string.Empty),
                FilePath = d.file_path ?? string.Empty,
                UploadedAt = d.uploaded_at
            }).ToList() ?? new List<LeaveDocumentViewModel>()
        };

        if (request.approved_by.HasValue)
        {
            var approver = await _context.Employee.FindAsync(request.approved_by.Value);
            viewModel.ApprovedByName = approver?.full_name ?? string.Empty;
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OverrideApprove(int id)
    {
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _leaveService.ApproveAsync(id, hrAdminId.Value);
        if (result)
        {
            _logger.LogInformation("HR Admin overrode and approved leave request {RequestId}", id);
            TempData["SuccessMessage"] = "Leave request approved (override).";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to approve leave request.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OverrideReject(int id)
    {
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _leaveService.RejectAsync(id, hrAdminId.Value);
        if (result)
        {
            _logger.LogInformation("HR Admin overrode and rejected leave request {RequestId}", id);
            TempData["SuccessMessage"] = "Leave request rejected (override).";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to reject leave request.";
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> OverrideFlagIrregular(int id)
    {
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var request = await _leaveService.GetByIdAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        // Verify this is NOT a special leave request (special leave is managed separately)
        var policy = await _context.LeavePolicy
            .FirstOrDefaultAsync(p => p.leave_type_id == request.leave_id && (p.is_active ?? true));

        if (policy != null && policy.requires_hr_admin_approval == true)
        {
            TempData["ErrorMessage"] = "Special leave requests are managed in the Special Leave section.";
            return RedirectToAction("Index");
        }

        var viewModel = new LeaveRequestViewModel
        {
            RequestId = request.request_id,
            EmployeeId = request.employee_id,
            EmployeeName = request.employee?.full_name ?? string.Empty,
            EmployeeEmail = request.employee?.email ?? string.Empty,
            LeaveType = request.leave?.leave_type ?? string.Empty,
            StartDate = request.start_date ?? DateOnly.FromDateTime(DateTime.Today),
            EndDate = request.end_date ?? DateOnly.FromDateTime(DateTime.Today),
            Duration = request.duration,
            Justification = request.justification,
            Status = request.status,
            ApprovedBy = request.approved_by,
            IsIrregular = request.is_irregular,
            IrregularityReason = request.irregularity_reason,
            CreatedAt = request.created_at
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OverrideFlagIrregular(int id, string irregularityReason)
    {
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(irregularityReason))
        {
            TempData["ErrorMessage"] = "Please provide a reason for flagging this leave request as irregular.";
            return RedirectToAction("OverrideFlagIrregular", new { id });
        }

        var request = await _leaveService.GetByIdAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        // Verify this is NOT a special leave request
        var policy = await _context.LeavePolicy
            .FirstOrDefaultAsync(p => p.leave_type_id == request.leave_id && (p.is_active ?? true));

        if (policy != null && policy.requires_hr_admin_approval == true)
        {
            TempData["ErrorMessage"] = "Special leave requests are managed in the Special Leave section.";
            return RedirectToAction("Index");
        }

        var result = await _leaveService.FlagAsIrregularAsync(id, hrAdminId.Value, irregularityReason);
        if (result)
        {
            _logger.LogInformation("HR Admin flagged leave request {RequestId} as irregular", id);
            TempData["SuccessMessage"] = "Leave request flagged as irregular.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to flag leave request as irregular.";
        }

        return RedirectToAction("Index");
    }
}


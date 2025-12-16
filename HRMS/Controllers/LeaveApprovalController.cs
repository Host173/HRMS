using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.Services;
using System.IO;
using System.Collections.Generic;

namespace HRMS.Controllers;

[Authorize]
public class LeaveApprovalController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<LeaveApprovalController> _logger;

    public LeaveApprovalController(
        ILeaveService leaveService,
        HrmsDbContext context,
        ILogger<LeaveApprovalController> logger)
    {
        _leaveService = leaveService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    public async Task<IActionResult> Index()
    {
        var managerId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!managerId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isLineManager = await AuthorizationHelper.IsLineManagerAsync(_context, managerId.Value);
        if (!isLineManager)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var allRequests = await _leaveService.GetByManagerIdAsync(managerId.Value);
        var allRequestsList = allRequests?.ToList() ?? new List<LeaveRequest>();

        // If there are no requests, short‑circuit to an empty view model list.
        if (!allRequestsList.Any())
        {
            return View(new List<LeaveRequestViewModel>());
        }
        
        // Get all policies for the leave types in these requests
        var leaveTypeIds = allRequestsList.Select(r => r.leave_id).Distinct().ToList();

        // If the DbSet is not configured (should not normally happen), avoid throwing.
        if (_context.LeavePolicy == null || leaveTypeIds.Count == 0)
        {
            return View(allRequestsList.Select(r => new LeaveRequestViewModel
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
                IsSpecialLeave = false,
                ApprovedByHR = false,
                Documents = r.LeaveDocument?.Select(d => new LeaveDocumentViewModel
                {
                    DocumentId = d.document_id,
                    FileName = Path.GetFileName(d.file_path ?? string.Empty),
                    FilePath = d.file_path ?? string.Empty,
                    UploadedAt = d.uploaded_at
                }).ToList() ?? new List<LeaveDocumentViewModel>()
            }).ToList());
        }

        // Query policies in a way that is safe for EF translation and empty lists.
        var policiesQuery = _context.LeavePolicy
            .Where(p => (p.is_active ?? true));

        policiesQuery = policiesQuery
            .Where(p => p.leave_type_id.HasValue && leaveTypeIds.Contains(p.leave_type_id.Value));

        var policies = await policiesQuery.ToListAsync();
        
        // Filter out special leave requests that require HR Admin approval (only show pending ones that line managers can approve)
        var requests = allRequestsList.Where(request =>
        {
            // If already approved/rejected, show it
            if (request.status != "Pending")
                return true;
            
            // Check if this is a special leave type that requires HR Admin approval
            var policy = policies.FirstOrDefault(p => p.leave_type_id == request.leave_id);
            
            // Only show requests that don't require HR Admin approval
            return policy == null || policy.requires_hr_admin_approval != true;
        }).ToList();
        
        // Get all approver IDs to check if they are HR Admins
        var approverIds = requests
            .Where(r => r.approved_by.HasValue)
            .Select(r => r.approved_by!.Value)
            .Distinct()
            .ToList();

        var hrAdminIds = new HashSet<int>();
        if (approverIds.Any())
        {
            var hrAdmins = await _context.Employee_Role
                .Include(er => er.role)
                .Where(er => approverIds.Contains(er.employee_id) && 
                             er.role.role_name == AuthorizationHelper.HRAdminRole)
                .Select(er => er.employee_id)
                .ToListAsync();
            hrAdminIds = new HashSet<int>(hrAdmins);
        }

        var viewModels = requests.Select(r =>
        {
            var policy = policies.FirstOrDefault(p => p.leave_type_id == r.leave_id);
            var isSpecialLeave = policy != null && policy.requires_hr_admin_approval == true;
            var approvedByHR = r.approved_by.HasValue && hrAdminIds.Contains(r.approved_by.Value) && isSpecialLeave;

            return new LeaveRequestViewModel
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
                IsSpecialLeave = isSpecialLeave,
                ApprovedByHR = approvedByHR,
                Documents = r.LeaveDocument?.Select(d => new LeaveDocumentViewModel
                {
                    DocumentId = d.document_id,
                    FileName = Path.GetFileName(d.file_path ?? string.Empty),
                    FilePath = d.file_path ?? string.Empty,
                    UploadedAt = d.uploaded_at
                }).ToList() ?? new List<LeaveDocumentViewModel>()
            };
        }).ToList();

        return View(viewModels);
    }

    [HttpGet]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    public async Task<IActionResult> Review(int id)
    {
        var managerId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!managerId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var request = await _leaveService.GetByIdAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        // Verify the request belongs to an employee managed by this manager
        var managedEmployeeIds = await _context.Employee
            .Where(e => e.manager_id == managerId.Value && e.is_active == true)
            .Select(e => e.employee_id)
            .ToListAsync();

        if (!managedEmployeeIds.Contains(request.employee_id))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Check if this is a special leave type
        var policy = await _context.LeavePolicy
            .FirstOrDefaultAsync(p => p.leave_type_id == request.leave_id && (p.is_active ?? true));
        var isSpecialLeave = policy != null && policy.requires_hr_admin_approval == true;

        // Check if approved by HR Admin
        var approvedByHR = false;
        if (request.approved_by.HasValue && isSpecialLeave)
        {
            approvedByHR = await AuthorizationHelper.IsHRAdminAsync(_context, request.approved_by.Value);
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
            IsSpecialLeave = isSpecialLeave,
            ApprovedByHR = approvedByHR,
            Documents = request.LeaveDocument?.Select(d => new LeaveDocumentViewModel
            {
                DocumentId = d.document_id,
                FileName = Path.GetFileName(d.file_path ?? string.Empty),
                FilePath = d.file_path ?? string.Empty,
                UploadedAt = d.uploaded_at
            }).ToList() ?? new List<LeaveDocumentViewModel>()
        };

        return View(viewModel);
    }

    [HttpPost]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var managerId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!managerId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Check if this is a special leave type that requires HR Admin approval
        var request = await _leaveService.GetByIdAsync(id);
        if (request != null)
        {
            var policy = await _context.LeavePolicy
                .Where(p => p.leave_type_id == request.leave_id && (p.is_active ?? true))
                .FirstOrDefaultAsync();

            if (policy != null && policy.requires_hr_admin_approval == true)
            {
                TempData["ErrorMessage"] = "This leave type requires HR Admin approval. You cannot approve this request.";
                return RedirectToAction("Index");
            }
        }

        var result = await _leaveService.ApproveAsync(id, managerId.Value);
        if (result)
        {
            _logger.LogInformation("Leave request {RequestId} approved by manager {ManagerId}", id, managerId.Value);
            TempData["SuccessMessage"] = "Leave request approved successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to approve leave request.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var managerId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!managerId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _leaveService.RejectAsync(id, managerId.Value);
        if (result)
        {
            _logger.LogInformation("Leave request {RequestId} rejected by manager {ManagerId}", id, managerId.Value);
            TempData["SuccessMessage"] = "Leave request rejected.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to reject leave request.";
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    public async Task<IActionResult> FlagIrregular(int id)
    {
        var managerId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!managerId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var request = await _leaveService.GetByIdAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        // Verify the request belongs to an employee managed by this manager
        var managedEmployeeIds = await _context.Employee
            .Where(e => e.manager_id == managerId.Value && e.is_active == true)
            .Select(e => e.employee_id)
            .ToListAsync();

        if (!managedEmployeeIds.Contains(request.employee_id))
        {
            return RedirectToAction("AccessDenied", "Account");
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
    [RequireRole(AuthorizationHelper.LineManagerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FlagIrregular(int id, string irregularityReason)
    {
        var managerId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!managerId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(irregularityReason))
        {
            TempData["ErrorMessage"] = "Please provide a reason for flagging this leave request as irregular.";
            return RedirectToAction("FlagIrregular", new { id });
        }

        var result = await _leaveService.FlagAsIrregularAsync(id, managerId.Value, irregularityReason);
        if (result)
        {
            _logger.LogInformation("Leave request {RequestId} flagged as irregular by manager {ManagerId}", id, managerId.Value);
            TempData["SuccessMessage"] = "Leave request flagged as irregular.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to flag leave request as irregular.";
        }

        return RedirectToAction("Index");
    }
}


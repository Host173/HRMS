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
public class SpecialLeaveController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<SpecialLeaveController> _logger;

    public SpecialLeaveController(
        ILeaveService leaveService,
        HrmsDbContext context,
        ILogger<SpecialLeaveController> logger)
    {
        _leaveService = leaveService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Verify user is HR Admin
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, hrAdminId.Value);
        if (!isHRAdmin)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Ensure DbSets are available
        if (_context.LeavePolicy == null || _context.LeaveRequest == null)
        {
            return View(new List<LeaveRequestViewModel>());
        }

        // Get all leave requests with their related data
        var allRequests = await _context.LeaveRequest
            .Include(lr => lr.employee)
            .Include(lr => lr.leave)
            .Include(lr => lr.LeaveDocument)
            .OrderByDescending(lr => lr.request_id)
            .ToListAsync();

        // Identify special leave requests by checking:
        // 1. Leave type name contains "Special" (case-insensitive)
        // 2. Or policy special_leave_type matches the leave type name
        var allPolicies = await _context.LeavePolicy.ToListAsync();
        
        var specialLeaveRequests = allRequests.Where(request =>
        {
            // Check if leave type name contains "Special" or "Holiday"
            if (request.leave != null && !string.IsNullOrEmpty(request.leave.leave_type))
            {
                var leaveTypeName = request.leave.leave_type;
                if (leaveTypeName.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                    leaveTypeName.Contains("Holiday", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Check if there's a policy with matching special_leave_type
            if (request.leave != null && !string.IsNullOrEmpty(request.leave.leave_type))
            {
                var matchingPolicy = allPolicies.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.special_leave_type) &&
                    p.special_leave_type.Equals(request.leave.leave_type, StringComparison.OrdinalIgnoreCase));
                
                if (matchingPolicy != null)
                {
                    return true;
                }
            }

            return false;
        }).ToList();

        // Get all approver IDs to check if they are HR Admins
        var approverIds = specialLeaveRequests
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

        // Convert to view models
        var viewModels = specialLeaveRequests.Select(r =>
        {
            var approvedByHR = r.approved_by.HasValue && hrAdminIds.Contains(r.approved_by.Value);
            
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
                IsSpecialLeave = true,
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
    public async Task<IActionResult> Review(int id)
    {
        // Verify user is HR Admin
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, hrAdminId.Value);
        if (!isHRAdmin)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Verify this is a special leave request
        var request = await _context.LeaveRequest
            .Include(lr => lr.leave)
            .FirstOrDefaultAsync(lr => lr.request_id == id);

        if (request == null)
        {
            return NotFound();
        }

        // Verify this is a special leave request by checking leave type name
        var isSpecialLeave = false;
        if (request.leave != null && !string.IsNullOrEmpty(request.leave.leave_type))
        {
            var leaveTypeName = request.leave.leave_type;
            isSpecialLeave = leaveTypeName.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                           leaveTypeName.Contains("Holiday", StringComparison.OrdinalIgnoreCase);
            
            // Also check policies
            if (!isSpecialLeave)
            {
                var allPolicies = await _context.LeavePolicy.ToListAsync();
                var matchingPolicy = allPolicies.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.special_leave_type) &&
                    p.special_leave_type.Equals(request.leave.leave_type, StringComparison.OrdinalIgnoreCase));
                isSpecialLeave = matchingPolicy != null;
            }
        }

        if (!isSpecialLeave)
        {
            TempData["ErrorMessage"] = "This is not a special leave request.";
            return RedirectToAction("Index");
        }

        var fullRequest = await _leaveService.GetByIdAsync(id);
        if (fullRequest == null)
        {
            return NotFound();
        }

        var viewModel = new LeaveRequestViewModel
        {
            RequestId = fullRequest.request_id,
            EmployeeId = fullRequest.employee_id,
            EmployeeName = fullRequest.employee?.full_name ?? string.Empty,
            EmployeeEmail = fullRequest.employee?.email ?? string.Empty,
            LeaveType = fullRequest.leave?.leave_type ?? string.Empty,
            StartDate = fullRequest.start_date ?? DateOnly.FromDateTime(DateTime.Today),
            EndDate = fullRequest.end_date ?? DateOnly.FromDateTime(DateTime.Today),
            Duration = fullRequest.duration,
            Justification = fullRequest.justification,
            Status = fullRequest.status,
            ApprovedBy = fullRequest.approved_by,
            IsIrregular = fullRequest.is_irregular,
            IrregularityReason = fullRequest.irregularity_reason,
            CreatedAt = fullRequest.created_at,
            Documents = fullRequest.LeaveDocument?.Select(d => new LeaveDocumentViewModel
            {
                DocumentId = d.document_id,
                FileName = Path.GetFileName(d.file_path ?? string.Empty),
                FilePath = d.file_path ?? string.Empty,
                UploadedAt = d.uploaded_at
            }).ToList() ?? new List<LeaveDocumentViewModel>()
        };

        if (fullRequest.approved_by.HasValue)
        {
            var approver = await _context.Employee.FindAsync(fullRequest.approved_by.Value);
            viewModel.ApprovedByName = approver?.full_name ?? string.Empty;
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verify user is HR Admin
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, hrAdminId.Value);
        if (!isHRAdmin)
        {
            TempData["ErrorMessage"] = "Only HR Administrators can approve special leave requests.";
            return RedirectToAction("AccessDenied", "Account");
        }

        // Verify this is a special leave request
        var request = await _context.LeaveRequest
            .Include(lr => lr.leave)
            .FirstOrDefaultAsync(lr => lr.request_id == id);

        if (request == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction("Index");
        }

        // Verify this is a special leave request by checking leave type name
        var isSpecialLeave = false;
        if (request.leave != null && !string.IsNullOrEmpty(request.leave.leave_type))
        {
            var leaveTypeName = request.leave.leave_type;
            isSpecialLeave = leaveTypeName.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                           leaveTypeName.Contains("Holiday", StringComparison.OrdinalIgnoreCase);
            
            // Also check policies
            if (!isSpecialLeave)
            {
                var allPolicies = await _context.LeavePolicy.ToListAsync();
                var matchingPolicy = allPolicies.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.special_leave_type) &&
                    p.special_leave_type.Equals(request.leave.leave_type, StringComparison.OrdinalIgnoreCase));
                isSpecialLeave = matchingPolicy != null;
            }
        }

        if (!isSpecialLeave)
        {
            TempData["ErrorMessage"] = "This is not a special leave request. Only special leave requests can be approved here.";
            return RedirectToAction("Index");
        }

        var result = await _leaveService.ApproveAsync(id, hrAdminId.Value);
        if (result)
        {
            _logger.LogInformation("HR Admin approved special leave request {RequestId}", id);
            TempData["SuccessMessage"] = "Special leave request approved successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to approve leave request.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verify user is HR Admin
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, hrAdminId.Value);
        if (!isHRAdmin)
        {
            TempData["ErrorMessage"] = "Only HR Administrators can reject special leave requests.";
            return RedirectToAction("AccessDenied", "Account");
        }

        // Verify this is a special leave request
        var request = await _context.LeaveRequest
            .Include(lr => lr.leave)
            .FirstOrDefaultAsync(lr => lr.request_id == id);

        if (request == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction("Index");
        }

        // Verify this is a special leave request by checking leave type name
        var isSpecialLeave = false;
        if (request.leave != null && !string.IsNullOrEmpty(request.leave.leave_type))
        {
            var leaveTypeName = request.leave.leave_type;
            isSpecialLeave = leaveTypeName.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                           leaveTypeName.Contains("Holiday", StringComparison.OrdinalIgnoreCase);
            
            // Also check policies
            if (!isSpecialLeave)
            {
                var allPolicies = await _context.LeavePolicy.ToListAsync();
                var matchingPolicy = allPolicies.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.special_leave_type) &&
                    p.special_leave_type.Equals(request.leave.leave_type, StringComparison.OrdinalIgnoreCase));
                isSpecialLeave = matchingPolicy != null;
            }
        }

        if (!isSpecialLeave)
        {
            TempData["ErrorMessage"] = "This is not a special leave request. Only special leave requests can be approved here.";
            return RedirectToAction("Index");
        }

        var result = await _leaveService.RejectAsync(id, hrAdminId.Value);
        if (result)
        {
            _logger.LogInformation("HR Admin rejected special leave request {RequestId}", id);
            TempData["SuccessMessage"] = "Special leave request rejected.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to reject leave request.";
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> FlagIrregular(int id)
    {
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verify user is HR Admin
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, hrAdminId.Value);
        if (!isHRAdmin)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Verify this is a special leave request
        var request = await _context.LeaveRequest
            .Include(lr => lr.leave)
            .Include(lr => lr.employee)
            .FirstOrDefaultAsync(lr => lr.request_id == id);

        if (request == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction("Index");
        }

        // Verify this is a special leave request by checking leave type name
        var isSpecialLeave = false;
        if (request.leave != null && !string.IsNullOrEmpty(request.leave.leave_type))
        {
            var leaveTypeName = request.leave.leave_type;
            isSpecialLeave = leaveTypeName.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                           leaveTypeName.Contains("Holiday", StringComparison.OrdinalIgnoreCase);
            
            // Also check policies
            if (!isSpecialLeave)
            {
                var allPolicies = await _context.LeavePolicy.ToListAsync();
                var matchingPolicy = allPolicies.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.special_leave_type) &&
                    p.special_leave_type.Equals(request.leave.leave_type, StringComparison.OrdinalIgnoreCase));
                isSpecialLeave = matchingPolicy != null;
            }
        }

        if (!isSpecialLeave)
        {
            TempData["ErrorMessage"] = "This is not a special leave request.";
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
    public async Task<IActionResult> FlagIrregular(int id, string irregularityReason)
    {
        var hrAdminId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!hrAdminId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verify user is HR Admin
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, hrAdminId.Value);
        if (!isHRAdmin)
        {
            TempData["ErrorMessage"] = "Only HR Administrators can flag special leave requests as irregular.";
            return RedirectToAction("AccessDenied", "Account");
        }

        if (string.IsNullOrWhiteSpace(irregularityReason))
        {
            TempData["ErrorMessage"] = "Please provide a reason for flagging this leave request as irregular.";
            return RedirectToAction("FlagIrregular", new { id });
        }

        // Verify this is a special leave request
        var request = await _context.LeaveRequest
            .Include(lr => lr.leave)
            .FirstOrDefaultAsync(lr => lr.request_id == id);

        if (request == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction("Index");
        }

        // Verify this is a special leave request by checking leave type name
        var isSpecialLeave = false;
        if (request.leave != null && !string.IsNullOrEmpty(request.leave.leave_type))
        {
            var leaveTypeName = request.leave.leave_type;
            isSpecialLeave = leaveTypeName.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                           leaveTypeName.Contains("Holiday", StringComparison.OrdinalIgnoreCase);
            
            // Also check policies
            if (!isSpecialLeave)
            {
                var allPolicies = await _context.LeavePolicy.ToListAsync();
                var matchingPolicy = allPolicies.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.special_leave_type) &&
                    p.special_leave_type.Equals(request.leave.leave_type, StringComparison.OrdinalIgnoreCase));
                isSpecialLeave = matchingPolicy != null;
            }
        }

        if (!isSpecialLeave)
        {
            TempData["ErrorMessage"] = "This is not a special leave request. Only special leave requests can be approved here.";
            return RedirectToAction("Index");
        }

        var result = await _leaveService.FlagAsIrregularAsync(id, hrAdminId.Value, irregularityReason);
        if (result)
        {
            _logger.LogInformation("HR Admin flagged special leave request {RequestId} as irregular", id);
            TempData["SuccessMessage"] = "Special leave request flagged as irregular.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to flag leave request as irregular.";
        }

        return RedirectToAction("Index");
    }
}


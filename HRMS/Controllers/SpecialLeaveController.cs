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
        // Ensure DbSets are available
        if (_context.LeavePolicy == null || _context.LeaveRequest == null)
        {
            return View(new List<LeaveRequestViewModel>());
        }

        // NOTE: Columns requires_hr_admin_approval, is_active, leave_type_id don't exist yet
        // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to add them
        // For now, return empty list since we cannot identify special leave requests
        // without these columns
        return View(new List<LeaveRequestViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        // Verify this is a special leave request
        var request = await _context.LeaveRequest
            .Include(lr => lr.leave)
            .FirstOrDefaultAsync(lr => lr.request_id == id);

        if (request == null)
        {
            return NotFound();
        }

        // NOTE: Cannot verify special leave without leave_type_id, is_active, requires_hr_admin_approval columns
        // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable this check
        // For now, show error since we cannot identify special leave requests
        TempData["ErrorMessage"] = "Special leave functionality requires database columns that don't exist yet. Please run SQL_ADD_LEAVE_POLICY_COLUMNS.sql";
        return RedirectToAction("Index");

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

        // Verify this is a special leave request
        var request = await _context.LeaveRequest
            .Include(lr => lr.leave)
            .FirstOrDefaultAsync(lr => lr.request_id == id);

        if (request == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction("Index");
        }

        // NOTE: Cannot verify special leave without leave_type_id, is_active, requires_hr_admin_approval columns
        // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable this check
        TempData["ErrorMessage"] = "Special leave functionality requires database columns that don't exist yet. Please run SQL_ADD_LEAVE_POLICY_COLUMNS.sql";
        return RedirectToAction("Index");

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

        // Verify this is a special leave request
        var request = await _context.LeaveRequest
            .Include(lr => lr.leave)
            .FirstOrDefaultAsync(lr => lr.request_id == id);

        if (request == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction("Index");
        }

        // NOTE: Cannot verify special leave without leave_type_id, is_active, requires_hr_admin_approval columns
        // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable this check
        TempData["ErrorMessage"] = "Special leave functionality requires database columns that don't exist yet. Please run SQL_ADD_LEAVE_POLICY_COLUMNS.sql";
        return RedirectToAction("Index");

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

        // NOTE: Cannot verify special leave without leave_type_id, is_active, requires_hr_admin_approval columns
        // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable this check
        TempData["ErrorMessage"] = "Special leave functionality requires database columns that don't exist yet. Please run SQL_ADD_LEAVE_POLICY_COLUMNS.sql";
        return RedirectToAction("Index");

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

        // NOTE: Cannot verify special leave without leave_type_id, is_active, requires_hr_admin_approval columns
        // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable this check
        TempData["ErrorMessage"] = "Special leave functionality requires database columns that don't exist yet. Please run SQL_ADD_LEAVE_POLICY_COLUMNS.sql";
        return RedirectToAction("Index");

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


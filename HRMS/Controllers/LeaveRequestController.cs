using System.Security.Claims;
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
public class LeaveRequestController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly HrmsDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LeaveRequestController> _logger;

    public LeaveRequestController(
        ILeaveService leaveService,
        HrmsDbContext context,
        IWebHostEnvironment environment,
        ILogger<LeaveRequestController> logger)
    {
        _leaveService = leaveService;
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var requests = await _leaveService.GetByEmployeeIdAsync(employeeId.Value);
        var balance = await _leaveService.GetLeaveBalanceAsync(employeeId.Value);

        ViewBag.LeaveBalance = balance;
        
        var viewModels = requests.Select(r => new LeaveRequestViewModel
        {
            RequestId = r.request_id,
            EmployeeId = r.employee_id,
            EmployeeName = r.employee?.full_name ?? string.Empty,
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
    public async Task<IActionResult> Create()
    {
        // Only show active leave types (those that exist in the Leave table)
        var leaveTypes = await _context.Leave
            .OrderBy(l => l.leave_type)
            .ToListAsync();
        
        var model = new CreateLeaveRequestViewModel
        {
            AvailableLeaveTypes = leaveTypes,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLeaveRequestViewModel model)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (model.EndDate < model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
            return View(model);
        }

        try
        {
            // Get leave type and policy
            var leaveType = await _context.Leave.FindAsync(model.LeaveTypeId);
            if (leaveType == null)
            {
                ModelState.AddModelError(string.Empty, "Selected leave type is not valid.");
                model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
                return View(model);
            }

            // Get active policy for this leave type
            var policy = await _context.LeavePolicy
                .Where(p => p.leave_type_id == model.LeaveTypeId && (p.is_active ?? true))
                .FirstOrDefaultAsync();

            // Calculate duration
            var duration = model.EndDate.DayNumber - model.StartDate.DayNumber + 1;

            // Enforce policy rules
            if (policy != null)
            {
                // Check minimum days
                if (policy.min_days_per_request.HasValue && duration < policy.min_days_per_request.Value)
                {
                    ModelState.AddModelError(string.Empty, 
                        $"Minimum days required for this leave type is {policy.min_days_per_request.Value} days.");
                    model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
                    return View(model);
                }

                // Check maximum days
                if (policy.max_days_per_request.HasValue && duration > policy.max_days_per_request.Value)
                {
                    ModelState.AddModelError(string.Empty, 
                        $"Maximum days allowed for this leave type is {policy.max_days_per_request.Value} days.");
                    model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
                    return View(model);
                }

                // Check documentation requirement
                if (policy.requires_documentation == true && (model.Attachments == null || !model.Attachments.Any()))
                {
                    ModelState.AddModelError(nameof(model.Attachments), 
                        "Documentation is required for this leave type. Please upload supporting documents.");
                    model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
                    return View(model);
                }

                // Check notice period
                if (policy.notice_period.HasValue)
                {
                    var daysUntilStart = model.StartDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
                    if (daysUntilStart < policy.notice_period.Value)
                    {
                        ModelState.AddModelError(nameof(model.StartDate), 
                            $"This leave type requires {policy.notice_period.Value} days advance notice.");
                        model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
                        return View(model);
                    }
                }
            }

            var leaveRequest = new LeaveRequest
            {
                employee_id = employeeId.Value,
                leave_id = model.LeaveTypeId,
                start_date = model.StartDate,
                end_date = model.EndDate,
                duration = duration,
                justification = model.Justification,
                status = "Pending",
                is_irregular = false,
                created_at = DateTime.UtcNow
            };

            await _leaveService.CreateAsync(leaveRequest);

            // Handle file uploads
            if (model.Attachments != null && model.Attachments.Any())
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "leave-documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var file in model.Attachments)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{leaveRequest.request_id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var document = new LeaveDocument
                        {
                            leave_request_id = leaveRequest.request_id,
                            file_path = $"/uploads/leave-documents/{fileName}",
                            uploaded_at = DateTime.UtcNow
                        };

                        _context.LeaveDocument.Add(document);
                    }
                }

                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Leave request created by employee {EmployeeId}: Request {RequestId}", employeeId.Value, leaveRequest.request_id);
            TempData["SuccessMessage"] = "Leave request submitted successfully!";
            return RedirectToAction("Index");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error creating leave request for employee {EmployeeId}", employeeId.Value);
            ModelState.AddModelError(string.Empty, "An error occurred while submitting the leave request. Please try again.");
            model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var request = await _leaveService.GetByIdAsync(id);
        if (request == null || request.employee_id != employeeId.Value)
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
}


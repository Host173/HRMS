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

        // Get approver names
        var approverEmployees = await _context.Employee
            .Where(e => approverIds.Contains(e.employee_id))
            .ToDictionaryAsync(e => e.employee_id, e => e.full_name ?? string.Empty);
        
        var viewModels = requests.Select(r =>
        {
            var approvedByHR = r.approved_by.HasValue && hrAdminIds.Contains(r.approved_by.Value);
            var approverName = r.approved_by.HasValue && approverEmployees.ContainsKey(r.approved_by.Value)
                ? approverEmployees[r.approved_by.Value]
                : null;

            return new LeaveRequestViewModel
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
                ApprovedByName = approverName,
                IsIrregular = r.is_irregular,
                IrregularityReason = r.irregularity_reason,
                CreatedAt = r.created_at,
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

    // Separate page: Leave history (milestone requirement)
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var requests = await _leaveService.GetByEmployeeIdAsync(employeeId.Value);
        
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

        // Get approver names
        var approverEmployees = await _context.Employee
            .Where(e => approverIds.Contains(e.employee_id))
            .ToDictionaryAsync(e => e.employee_id, e => e.full_name ?? string.Empty);
        
        var viewModels = requests.Select(r =>
        {
            var approvedByHR = r.approved_by.HasValue && hrAdminIds.Contains(r.approved_by.Value);
            var approverName = r.approved_by.HasValue && approverEmployees.ContainsKey(r.approved_by.Value)
                ? approverEmployees[r.approved_by.Value]
                : null;

            return new LeaveRequestViewModel
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
                ApprovedByName = approverName,
                IsIrregular = r.is_irregular,
                IrregularityReason = r.irregularity_reason,
                CreatedAt = r.created_at,
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

    // Separate page: Leave balance (milestone requirement)
    [HttpGet]
    public async Task<IActionResult> Balance()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var balance = await _leaveService.GetLeaveBalanceAsync(employeeId.Value);
        return View(balance);
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
            // Note: Since leave_type_id and is_active are not mapped in the database (see HrmsDbContext),
            // we cannot filter policies by leave type. Policy validation is skipped until SQL_ADD_LEAVE_POLICY_COLUMNS.sql is run.
            // Try to find a policy by matching the leave type name in the policy name or special_leave_type
            LeavePolicy? policy = null;
            if (leaveType != null)
            {
                var allPolicies = await _context.LeavePolicy.ToListAsync();
                // Try to match by special_leave_type or policy name containing leave type name
                policy = allPolicies
                    .Where(p => !string.IsNullOrEmpty(p.special_leave_type) && 
                               p.special_leave_type.Equals(leaveType.leave_type, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                
                // If no match by special_leave_type, try matching by policy name
                if (policy == null)
                {
                    policy = allPolicies
                        .Where(p => !string.IsNullOrEmpty(p.name) && 
                                   p.name.Contains(leaveType.leave_type, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
                }
            }

            // Calculate duration
            var duration = model.EndDate.DayNumber - model.StartDate.DayNumber + 1;

            // Enforce policy rules (only if policy is found)
            // Note: min_days_per_request, max_days_per_request, and requires_documentation are not mapped in the database
            // So we can only validate notice_period which is mapped
            if (policy != null)
            {
                // Check notice period (this property IS mapped and available)
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
                
                // Note: The following validations are skipped because the columns don't exist in the database:
                // - min_days_per_request (not mapped)
                // - max_days_per_request (not mapped)  
                // - requires_documentation (not mapped)
                // Run SQL_ADD_LEAVE_POLICY_COLUMNS.sql to enable these validations
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

            // Verify request_id was set by the service
            if (leaveRequest.request_id == 0)
            {
                throw new InvalidOperationException("Leave request was created but request ID was not set.");
            }

            // Handle file uploads
            if (model.Attachments != null && model.Attachments.Any())
            {
                if (string.IsNullOrEmpty(_environment.WebRootPath))
                {
                    throw new InvalidOperationException("WebRootPath is not configured. Cannot save file uploads.");
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "leave-documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    catch (System.Exception dirEx)
                    {
                        _logger.LogError(dirEx, "Error creating uploads directory: {Path}", uploadsFolder);
                        throw new InvalidOperationException($"Cannot create uploads directory: {dirEx.Message}", dirEx);
                    }
                }

                foreach (var file in model.Attachments)
                {
                    if (file != null && file.Length > 0)
                    {
                        // Validate file size (10MB limit)
                        const long maxFileSize = 10 * 1024 * 1024; // 10MB
                        if (file.Length > maxFileSize)
                        {
                            ModelState.AddModelError(nameof(model.Attachments), 
                                $"File '{file.FileName}' exceeds the maximum size of 10MB.");
                            model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
                            return View(model);
                        }

                        // Validate file extension
                        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                        var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError(nameof(model.Attachments), 
                                $"File '{file.FileName}' has an invalid extension. Allowed extensions: PDF, DOC, DOCX, JPG, PNG.");
                            model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
                            return View(model);
                        }

                        var fileName = $"{leaveRequest.request_id}_{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        try
                        {
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
                        catch (System.Exception fileEx)
                        {
                            _logger.LogError(fileEx, "Error saving file {FileName} for leave request {RequestId}", 
                                file.FileName, leaveRequest.request_id);
                            ModelState.AddModelError(nameof(model.Attachments), 
                                $"Error uploading file '{file.FileName}': {fileEx.Message}");
                            model.AvailableLeaveTypes = await _context.Leave.ToListAsync();
                            return View(model);
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                }
            }

            _logger.LogInformation("Leave request created by employee {EmployeeId}: Request {RequestId}", employeeId.Value, leaveRequest.request_id);
            TempData["SuccessMessage"] = "Leave request submitted successfully!";
            return RedirectToAction("Index");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error creating leave request for employee {EmployeeId}. Exception: {ExceptionMessage}. StackTrace: {StackTrace}", 
                employeeId.Value, ex.Message, ex.StackTrace);
            
            // Show more specific error messages to help diagnose the issue
            var errorMessage = "An error occurred while submitting the leave request. Please try again.";
            if (ex is InvalidOperationException)
            {
                errorMessage = ex.Message;
            }
            else if (ex.InnerException != null)
            {
                errorMessage = $"Error: {ex.InnerException.Message}";
            }
            
            ModelState.AddModelError(string.Empty, errorMessage);
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
            
            // Check if approver is HR Admin
            viewModel.ApprovedByHR = await AuthorizationHelper.IsHRAdminAsync(_context, request.approved_by.Value);
        }

        return View(viewModel);
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.Services;

namespace HRMS.Controllers;

[Authorize]
public class Component5Controller : Controller
{
    private readonly HrmsDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<Component5Controller> _logger;

    public Component5Controller(
        HrmsDbContext context,
        INotificationService notificationService,
        ILogger<Component5Controller> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    #region Notifications

    /// <summary>
    /// View all notifications for the current employee
    /// All employees can access this
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MyNotifications()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("Retrieving notifications for employee ID: {EmployeeId}", employeeId.Value);

        // Query directly from Employee_Notification for this employee, then include the Notification
        // Use AsNoTracking for better performance, but ensure navigation properties are loaded
        var employeeNotifications = await _context.Employee_Notification
            .Include(en => en.notification)
            .Where(en => en.employee_id == employeeId.Value && en.notification != null)
            .OrderByDescending(en => en.notification != null ? en.notification.timestamp : DateTime.MinValue)
            .ToListAsync();

        _logger.LogInformation("Found {Count} Employee_Notification records for employee {EmployeeId}", 
            employeeNotifications.Count, employeeId.Value);

        var notificationViewModels = employeeNotifications
            .Where(en => en.notification != null)
            .Select(en =>
            {
                var n = en.notification!;
                return new NotificationViewModel
                {
                    NotificationId = n.notification_id,
                    MessageContent = n.message_content ?? string.Empty,
                    IsRead = en.delivery_status == "Read" || (n.read_status == true),
                    Timestamp = n.timestamp,
                    NotificationType = n.notification_type,
                    Urgency = n.urgency
                };
            })
            .ToList();

        _logger.LogInformation("Created {Count} NotificationViewModel objects for employee {EmployeeId}", 
            notificationViewModels.Count, employeeId.Value);

        // Check if user can send notifications (Line Managers, HR Admins, System Admins)
        var isLineManager = await AuthorizationHelper.IsLineManagerAsync(_context, employeeId.Value);
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
        ViewBag.CanSendNotifications = isLineManager || isHRAdmin || isSystemAdmin;

        // Debug: Log what we're returning
        if (notificationViewModels.Count == 0)
        {
            _logger.LogWarning("No notifications found for employee {EmployeeId}. Checking database directly...", employeeId.Value);
            
            // Direct database check
            var directCheck = await _context.Employee_Notification
                .Where(en => en.employee_id == employeeId.Value)
                .CountAsync();
            _logger.LogInformation("Direct Employee_Notification count for employee {EmployeeId}: {Count}", employeeId.Value, directCheck);
            
            var allNotifications = await _context.Notification.CountAsync();
            _logger.LogInformation("Total notifications in database: {Count}", allNotifications);
            
            var allLinks = await _context.Employee_Notification.CountAsync();
            _logger.LogInformation("Total Employee_Notification links in database: {Count}", allLinks);
        }

        return View(notificationViewModels);
    }

    /// <summary>
    /// Send notification form (GET)
    /// Line Managers, HR Admins, and System Admins can send notifications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SendNotification()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isLineManager = await AuthorizationHelper.IsLineManagerAsync(_context, employeeId.Value);
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);

        if (!isLineManager && !isHRAdmin && !isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to send notifications.";
            return RedirectToAction("MyNotifications");
        }

        var model = new SendNotificationViewModel();
        ViewBag.IsManager = isLineManager;
        ViewBag.IsHRAdmin = isHRAdmin;
        ViewBag.IsSystemAdmin = isSystemAdmin;

        // Get team members if line manager
        if (isLineManager)
        {
            var teamMembers = await _context.Employee
                .Where(e => e.manager_id == employeeId.Value && e.is_active == true)
                .ToListAsync();
            ViewBag.TeamMembers = teamMembers;
        }

        // Get all employees and departments for HR/System Admins
        if (isHRAdmin || isSystemAdmin)
        {
            var allEmployees = await _context.Employee
                .Where(e => e.is_active == true)
                .Include(e => e.department)
                .ToListAsync();
            ViewBag.AllEmployees = allEmployees;

            var departments = await _context.Department.ToListAsync();
            ViewBag.Departments = departments;
        }

        return View(model);
    }

    /// <summary>
    /// Send notification (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNotification(SendNotificationViewModel model)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var isLineManager = await AuthorizationHelper.IsLineManagerAsync(_context, employeeId.Value);
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);

        if (!isLineManager && !isHRAdmin && !isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to send notifications.";
            return RedirectToAction("MyNotifications");
        }

        try
        {
            var notification = new Notification
            {
                message_content = model.Message,
                notification_type = model.NotificationType,
                urgency = model.Urgency,
                timestamp = DateTime.UtcNow,
                read_status = false
            };

            _context.Notification.Add(notification);
            var saveResult = await _context.SaveChangesAsync();
            
            _logger.LogInformation("Notification created with ID: {NotificationId}, Message: {Message}, SaveChanges returned: {SaveResult}", 
                notification.notification_id, model.Message, saveResult);
            
            // Verify notification was saved and has an ID
            if (notification.notification_id == 0)
            {
                _logger.LogError("Notification was not saved properly - notification_id is still 0!");
                TempData["ErrorMessage"] = "Failed to create notification. Please try again.";
                return View(model);
            }

            List<int> targetEmployeeIds = new();

            // Determine recipients - priority: SendToTeam > ReceiverId > DepartmentId
            if (model.SendToTeam && isLineManager)
            {
                // Send to all team members
                var teamMembers = await _context.Employee
                    .Where(e => e.manager_id == employeeId.Value && e.is_active == true)
                    .Select(e => e.employee_id)
                    .ToListAsync();
                targetEmployeeIds.AddRange(teamMembers);
                _logger.LogInformation("Sending to entire team: {Count} team members", teamMembers.Count);
            }
            else if (model.ReceiverId.HasValue && model.ReceiverId.Value > 0)
            {
                // Send to specific employee
                // Verify employee exists and is active
                var employee = await _context.Employee
                    .FirstOrDefaultAsync(e => e.employee_id == model.ReceiverId.Value && e.is_active == true);
                
                if (employee != null)
                {
                    // For Line Managers, verify the employee is in their team
                    if (isLineManager && employee.manager_id != employeeId.Value)
                    {
                        TempData["ErrorMessage"] = "You can only send notifications to your team members.";
                        // Reload view data
                        ViewBag.IsManager = isLineManager;
                        ViewBag.IsHRAdmin = isHRAdmin;
                        ViewBag.IsSystemAdmin = isSystemAdmin;
                        
                        if (isLineManager)
                        {
                            var teamMembers = await _context.Employee
                                .Where(e => e.manager_id == employeeId.Value && e.is_active == true)
                                .ToListAsync();
                            ViewBag.TeamMembers = teamMembers;
                        }
                        
                        if (isHRAdmin || isSystemAdmin)
                        {
                            var allEmployees = await _context.Employee
                                .Where(e => e.is_active == true)
                                .Include(e => e.department)
                                .ToListAsync();
                            ViewBag.AllEmployees = allEmployees;
                            var departments = await _context.Department.ToListAsync();
                            ViewBag.Departments = departments;
                        }
                        
                        return View(model);
                    }
                    
                    targetEmployeeIds.Add(model.ReceiverId.Value);
                    _logger.LogInformation("Sending to specific employee: {EmployeeId} ({EmployeeName})", 
                        employee.employee_id, employee.full_name ?? $"{employee.first_name} {employee.last_name}");
                }
                else
                {
                    TempData["ErrorMessage"] = "Selected employee not found or is inactive.";
                    // Reload view data
                    ViewBag.IsManager = isLineManager;
                    ViewBag.IsHRAdmin = isHRAdmin;
                    ViewBag.IsSystemAdmin = isSystemAdmin;
                    
                    if (isLineManager)
                    {
                        var teamMembers = await _context.Employee
                            .Where(e => e.manager_id == employeeId.Value && e.is_active == true)
                            .ToListAsync();
                        ViewBag.TeamMembers = teamMembers;
                    }
                    
                    if (isHRAdmin || isSystemAdmin)
                    {
                        var allEmployees = await _context.Employee
                            .Where(e => e.is_active == true)
                            .Include(e => e.department)
                            .ToListAsync();
                        ViewBag.AllEmployees = allEmployees;
                        var departments = await _context.Department.ToListAsync();
                        ViewBag.Departments = departments;
                    }
                    
                    return View(model);
                }
            }
            else if (model.DepartmentId.HasValue && (isHRAdmin || isSystemAdmin))
            {
                // Send to all employees in department
                var deptEmployees = await _context.Employee
                    .Where(e => e.department_id == model.DepartmentId && e.is_active == true)
                    .Select(e => e.employee_id)
                    .ToListAsync();
                targetEmployeeIds.AddRange(deptEmployees);
                _logger.LogInformation("Sending to department: {Count} employees", deptEmployees.Count);
            }

            // Validate that we have at least one recipient
            if (targetEmployeeIds.Count == 0)
            {
                _logger.LogWarning("No recipients selected. SendToTeam: {SendToTeam}, ReceiverId: {ReceiverId}, DepartmentId: {DepartmentId}", 
                    model.SendToTeam, model.ReceiverId, model.DepartmentId);
                TempData["ErrorMessage"] = "Please select at least one recipient for the notification. Either check 'Send to my entire team' or select a specific team member.";
                // Reload view data
                ViewBag.IsManager = isLineManager;
                ViewBag.IsHRAdmin = isHRAdmin;
                ViewBag.IsSystemAdmin = isSystemAdmin;
                
                if (isLineManager)
                {
                    var teamMembers = await _context.Employee
                        .Where(e => e.manager_id == employeeId.Value && e.is_active == true)
                        .ToListAsync();
                    ViewBag.TeamMembers = teamMembers;
                }
                
                if (isHRAdmin || isSystemAdmin)
                {
                    var allEmployees = await _context.Employee
                        .Where(e => e.is_active == true)
                        .Include(e => e.department)
                        .ToListAsync();
                    ViewBag.AllEmployees = allEmployees;
                    var departments = await _context.Department.ToListAsync();
                    ViewBag.Departments = departments;
                }
                
                return View(model);
            }

            // Create employee-notification links
            var linksCreated = 0;
            foreach (var empId in targetEmployeeIds)
            {
                // Check if link already exists (shouldn't happen, but safety check)
                var existingLink = await _context.Employee_Notification
                    .FirstOrDefaultAsync(en => en.employee_id == empId && en.notification_id == notification.notification_id);
                
                if (existingLink == null)
                {
                    var empNotif = new Employee_Notification
                    {
                        employee_id = empId,
                        notification_id = notification.notification_id,
                        delivery_status = "Unread"
                    };
                    _context.Employee_Notification.Add(empNotif);
                    linksCreated++;
                    _logger.LogInformation("Added Employee_Notification link: NotificationId={NotificationId}, EmployeeId={EmployeeId}, Status=Unread", 
                        notification.notification_id, empId);
                }
                else
                {
                    _logger.LogWarning("Employee-Notification link already exists for employee {EmployeeId} and notification {NotificationId}", 
                        empId, notification.notification_id);
                }
            }

            if (linksCreated > 0)
            {
                var savedCount = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChanges completed. Expected {ExpectedCount} links, SaveChanges returned: {SavedCount}", 
                    linksCreated, savedCount);
                
                // Verify the links were actually saved
                var verifyLinks = await _context.Employee_Notification
                    .Where(en => en.notification_id == notification.notification_id)
                    .ToListAsync();
                _logger.LogInformation("Verification: Found {Count} Employee_Notification links for notification {NotificationId}", 
                    verifyLinks.Count, notification.notification_id);
                
                foreach (var link in verifyLinks)
                {
                    _logger.LogInformation("  - Link ID: {LinkId}, Employee ID: {EmployeeId}, Status: {Status}", 
                        link.employee_notification_id, link.employee_id, link.delivery_status);
                }
            }
            else
            {
                _logger.LogWarning("No Employee_Notification links were created! This should not happen.");
            }

            // Final verification before redirect
            var finalCheck = await _context.Employee_Notification
                .Where(en => en.notification_id == notification.notification_id)
                .Select(en => en.employee_id)
                .ToListAsync();
            
            _logger.LogInformation("Final check: Notification {NotificationId} is linked to {Count} employees: [{EmployeeIds}]", 
                notification.notification_id, finalCheck.Count, string.Join(", ", finalCheck));

            TempData["SuccessMessage"] = $"Notification sent successfully to {targetEmployeeIds.Count} recipient(s).";
            return RedirectToAction("MyNotifications");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            TempData["ErrorMessage"] = "An error occurred while sending the notification.";
            return View(model);
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var empNotif = await _context.Employee_Notification
            .FirstOrDefaultAsync(en => en.employee_id == employeeId.Value && en.notification_id == notificationId);

        if (empNotif != null)
        {
            empNotif.delivery_status = "Read";
            empNotif.delivered_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("MyNotifications");
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var unreadNotifications = await _context.Employee_Notification
            .Where(en => en.employee_id == employeeId.Value && 
                        (en.delivery_status != "Read" || en.delivery_status == null))
            .ToListAsync();

        foreach (var empNotif in unreadNotifications)
        {
            empNotif.delivery_status = "Read";
            empNotif.delivered_at = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "All notifications marked as read.";
        return RedirectToAction("MyNotifications");
    }

    /// <summary>
    /// Debug endpoint to check notification data (temporary - remove in production)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> DebugNotifications(int? employeeId = null)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        var targetEmployeeId = employeeId ?? currentEmployeeId;

        if (!targetEmployeeId.HasValue)
        {
            return Json(new { error = "No employee ID provided" });
        }

        var debugInfo = new
        {
            EmployeeId = targetEmployeeId.Value,
            Notifications = await _context.Notification
                .Select(n => new
                {
                    n.notification_id,
                    n.message_content,
                    n.timestamp,
                    n.urgency,
                    n.notification_type,
                    LinkedEmployees = n.Employee_Notification.Select(en => en.employee_id).ToList()
                })
                .ToListAsync(),
            EmployeeNotifications = await _context.Employee_Notification
                .Where(en => en.employee_id == targetEmployeeId.Value)
                .Select(en => new
                {
                    en.employee_notification_id,
                    en.employee_id,
                    en.notification_id,
                    en.delivery_status,
                    NotificationMessage = en.notification.message_content
                })
                .ToListAsync(),
            TotalNotifications = await _context.Notification.CountAsync(),
            TotalLinks = await _context.Employee_Notification.CountAsync()
        };

        return Json(debugInfo);
    }

    #endregion

    #region Analytics

    /// <summary>
    /// Analytics dashboard
    /// HR Admins can generate reports
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Analytics()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);

        if (!isHRAdmin && !isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to access analytics.";
            return RedirectToAction("Index", "Home");
        }

        // Get department statistics - optimized query
        var deptStats = await _context.Department
            .Include(d => d.Employee)
            .Select(d => new DepartmentStatisticsViewModel
            {
                DepartmentName = d.department_name ?? "Unknown",
                TotalEmployees = d.Employee.Count,
                ActiveEmployees = d.Employee.Count(e => e.is_active == true),
                InactiveEmployees = d.Employee.Count(e => e.is_active != true || e.is_active == null)
            })
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();

        ViewBag.DepartmentStats = deptStats;

        // Get departments for filter dropdown
        var departments = await _context.Department
            .OrderBy(d => d.department_name)
            .ToListAsync();
        ViewBag.Departments = departments;

        return View();
    }

    /// <summary>
    /// Generate compliance report with optional search/filter
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateComplianceReport(string reportType, string? searchTerm = null, int? departmentId = null)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);

        if (!isHRAdmin && !isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to generate reports.";
            return RedirectToAction("Analytics");
        }

        var report = new ComplianceReportViewModel
        {
            ReportType = reportType == "contract" ? "Contract Compliance" : "Attendance Compliance"
        };

        if (reportType == "contract")
        {
            // Query from Employee side since Contract has Employee collection
            var employeesQuery = _context.Employee
                .Include(e => e.contract)
                .Include(e => e.department)
                .Where(e => e.contract != null)
                .AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                employeesQuery = employeesQuery.Where(e => 
                    (e.full_name != null && e.full_name.Contains(searchTerm)) ||
                    (e.email != null && e.email.Contains(searchTerm)) ||
                    (e.first_name != null && e.first_name.Contains(searchTerm)) ||
                    (e.last_name != null && e.last_name.Contains(searchTerm))
                );
            }

            // Apply department filter if provided
            if (departmentId.HasValue)
            {
                employeesQuery = employeesQuery.Where(e => e.department_id == departmentId.Value);
            }

            var employees = await employeesQuery.ToListAsync();
            var contracts = employees.Select(e => e.contract).Where(c => c != null).Distinct().ToList();
            
            report.TotalRecords = contracts.Count;
            // Assume compliant if contract is active and not expired
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            report.CompliantRecords = contracts.Count(c => 
                c != null && c.start_date.HasValue && c.start_date.Value <= now && 
                (c.end_date == null || c.end_date >= now));
            report.NonCompliantRecords = report.TotalRecords - report.CompliantRecords;
        }
        else if (reportType == "attendance")
        {
            var attendanceQuery = _context.Attendance
                .Include(a => a.employee)
                    .ThenInclude(e => e.department)
                .AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                attendanceQuery = attendanceQuery.Where(a => 
                    (a.employee != null && (
                        a.employee.full_name != null && a.employee.full_name.Contains(searchTerm) ||
                        a.employee.email != null && a.employee.email.Contains(searchTerm) ||
                        (a.employee.first_name != null && a.employee.first_name.Contains(searchTerm)) ||
                        (a.employee.last_name != null && a.employee.last_name.Contains(searchTerm))
                    ))
                );
            }

            // Apply department filter if provided
            if (departmentId.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => 
                    a.employee != null && a.employee.department_id == departmentId.Value);
            }

            var attendances = await attendanceQuery.ToListAsync();
            report.TotalRecords = attendances.Count;
            // Simplified: assume compliant if attendance exists
            report.CompliantRecords = attendances.Count;
            report.NonCompliantRecords = 0;
        }

        ViewBag.ComplianceReport = report;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.SelectedDepartmentId = departmentId;

        // Refresh department stats - optimized query
        var deptStats = await _context.Department
            .Include(d => d.Employee)
            .Select(d => new DepartmentStatisticsViewModel
            {
                DepartmentName = d.department_name ?? "Unknown",
                TotalEmployees = d.Employee.Count,
                ActiveEmployees = d.Employee.Count(e => e.is_active == true),
                InactiveEmployees = d.Employee.Count(e => e.is_active != true || e.is_active == null)
            })
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();

        ViewBag.DepartmentStats = deptStats;

        // Get departments for filter dropdown
        var departments = await _context.Department
            .OrderBy(d => d.department_name)
            .ToListAsync();
        ViewBag.Departments = departments;

        return View("Analytics");
    }

    /// <summary>
    /// Generate diversity report with optional search/filter
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateDiversityReport(string? searchTerm = null, int? departmentId = null)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);
        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);

        if (!isHRAdmin && !isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to generate reports.";
            return RedirectToAction("Analytics");
        }

        var employeesQuery = _context.Employee
            .Where(e => e.is_active == true)
            .Include(e => e.department)
            .Include(e => e.position)
            .AsQueryable();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            employeesQuery = employeesQuery.Where(e => 
                (e.full_name != null && e.full_name.Contains(searchTerm)) ||
                (e.email != null && e.email.Contains(searchTerm)) ||
                (e.first_name != null && e.first_name.Contains(searchTerm)) ||
                (e.last_name != null && e.last_name.Contains(searchTerm)) ||
                (e.position != null && e.position.position_title != null && e.position.position_title.Contains(searchTerm))
            );
        }

        // Apply department filter if provided
        if (departmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.department_id == departmentId.Value);
        }

        var employees = await employeesQuery.ToListAsync();

        var report = new DiversityReportViewModel
        {
            TotalEmployees = employees.Count
        };

        // Department distribution
        report.DepartmentDistribution = employees
            .Where(e => e.department != null)
            .GroupBy(e => e.department!.department_name ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        // Position distribution
        report.PositionDistribution = employees
            .Where(e => e.position != null)
            .GroupBy(e => e.position!.position_title ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        // Department-wise employee statistics
        report.DepartmentStatistics = employees
            .GroupBy(e => e.department)
            .Select(g => new DepartmentStatisticsViewModel
            {
                DepartmentName = g.Key?.department_name ?? "No Department",
                TotalEmployees = g.Count(),
                ActiveEmployees = g.Count(e => e.is_active == true),
                InactiveEmployees = g.Count(e => e.is_active != true || e.is_active == null)
            })
            .OrderBy(d => d.DepartmentName)
            .ToList();

        ViewBag.DiversityReport = report;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.SelectedDepartmentId = departmentId;

        // Refresh department stats - optimized query
        var deptStats = await _context.Department
            .Include(d => d.Employee)
            .Select(d => new DepartmentStatisticsViewModel
            {
                DepartmentName = d.department_name ?? "Unknown",
                TotalEmployees = d.Employee.Count,
                ActiveEmployees = d.Employee.Count(e => e.is_active == true),
                InactiveEmployees = d.Employee.Count(e => e.is_active != true || e.is_active == null)
            })
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();

        ViewBag.DepartmentStats = deptStats;

        // Get departments for filter dropdown
        var departments = await _context.Department
            .OrderBy(d => d.department_name)
            .ToListAsync();
        ViewBag.Departments = departments;

        return View("Analytics");
    }

    #endregion

    #region Hierarchy

    /// <summary>
    /// View organizational hierarchy
    /// System Admins can view and reassign employees
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Hierarchy()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);

        if (!isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to view the organizational hierarchy.";
            return RedirectToAction("Index", "Home");
        }

        // Build hierarchy
        var allEmployees = await _context.Employee
            .Where(e => e.is_active == true)
            .Include(e => e.department)
            .Include(e => e.position)
            .ToListAsync();

        var hierarchy = BuildHierarchy(allEmployees);

        ViewBag.Hierarchy = hierarchy;
        ViewBag.AllEmployees = allEmployees;
        ViewBag.Departments = await _context.Department.ToListAsync();

        return View();
    }

    /// <summary>
    /// Reassign employee to new department or manager
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReassignEmployee(int EmployeeId, int? NewDepartmentId, int? NewManagerId)
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);

        if (!isSystemAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to reassign employees.";
            return RedirectToAction("Hierarchy");
        }

        var employee = await _context.Employee.FindAsync(EmployeeId);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Employee not found.";
            return RedirectToAction("Hierarchy");
        }

        try
        {
            if (NewDepartmentId.HasValue)
            {
                employee.department_id = NewDepartmentId.Value;
            }

            if (NewManagerId.HasValue)
            {
                employee.manager_id = NewManagerId.Value;
            }
            else if (NewManagerId == 0) // Explicitly set to null
            {
                employee.manager_id = null;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Employee {employee.full_name ?? $"{employee.first_name} {employee.last_name}"} has been reassigned successfully.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error reassigning employee");
            TempData["ErrorMessage"] = "An error occurred while reassigning the employee.";
        }

        return RedirectToAction("Hierarchy");
    }

    /// <summary>
    /// Build hierarchy tree from employees
    /// </summary>
    private List<HierarchyNodeViewModel> BuildHierarchy(List<Employee> employees)
    {
        var nodes = new Dictionary<int, HierarchyNodeViewModel>();
        var roots = new List<HierarchyNodeViewModel>();

        // Create nodes for all employees
        foreach (var emp in employees)
        {
            var node = new HierarchyNodeViewModel
            {
                EmployeeName = emp.full_name ?? $"{emp.first_name} {emp.last_name}",
                Position = emp.position?.position_title,
                Department = emp.department?.department_name,
                Level = 0,
                Subordinates = new List<HierarchyNodeViewModel>()
            };
            nodes[emp.employee_id] = node;
        }

        // Build tree structure
        foreach (var emp in employees)
        {
            var node = nodes[emp.employee_id];
            
            if (emp.manager_id.HasValue && nodes.ContainsKey(emp.manager_id.Value))
            {
                var managerNode = nodes[emp.manager_id.Value];
                managerNode.Subordinates.Add(node);
                node.Level = managerNode.Level + 1;
            }
            else
            {
                // Root node (no manager)
                roots.Add(node);
            }
        }

        return roots;
    }

    #endregion
}


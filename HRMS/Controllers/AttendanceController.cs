using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HRMS.Data;
using HRMS.Models;
using HRMS.Helpers;
using HRMS.Services;

namespace HRMS.Controllers;

public class ClockOutRequest
{
    public int AttendanceId { get; set; }
}

[Authorize]
public class AttendanceController : Controller
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<AttendanceController> _logger;
    private readonly INotificationService _notificationService;

    public AttendanceController(HrmsDbContext context, ILogger<AttendanceController> logger, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    private int GetCurrentEmployeeId()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
            return 0;

        var employee = _context.Employee
            .FirstOrDefault(e => e.email != null && e.email.ToLower() == email.ToLower());
        return employee?.employee_id ?? 0;
    }

    private bool IsManager()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return false;

        // Check if employee is a manager (has manager_id set or is a LineManager)
        var employee = _context.Employee
            .Include(e => e.LineManager)
            .FirstOrDefault(e => e.employee_id == employeeId);
        
        return employee?.LineManager != null || 
               _context.Employee.Any(e => e.manager_id == employeeId);
    }

    private async Task<bool> IsSystemAdminAsync()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return false;

        return await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId);
    }
    
    // Keep synchronous version for backward compatibility, but make it async internally
    private bool IsSystemAdmin()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return false;

        // Use the async helper method synchronously (not ideal but maintains compatibility)
        return _context.SystemAdministrator.Any(sa => sa.employee_id == employeeId) ||
               _context.Employee_Role
                   .Include(er => er.role)
                   .Any(er => er.employee_id == employeeId && er.role.role_name == AuthorizationHelper.SystemAdminRole);
    }

    private async Task<bool> IsHRAdminAsync()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return false;

        return await AuthorizationHelper.IsHRAdminAsync(_context, employeeId);
    }

    /// <summary>
    /// Attendance Index - Role-based access control:
    /// - Regular Employees: Can only see their own attendance records
    /// - Managers: Can see their team's attendance records (or their own if myAttendance=true)
    /// - System Admin & HR Admin: Can see all attendance records
    /// </summary>
    public async Task<IActionResult> Index(bool? myAttendance = false)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
            return RedirectToAction("Login", "Account");

        var isSystemAdmin = await IsSystemAdminAsync();
        var isHRAdmin = await IsHRAdminAsync();
        var isManager = IsManager();

        List<Attendance> records;
        
        if (isSystemAdmin)
        {
            // System Admin sees all attendance records (including absent records)
            records = await _context.Attendance
                .Include(a => a.employee)
                .Include(a => a.shift)
                .Include(a => a.exception)
                .Where(a => a.entry_time.HasValue) // Include all records with entry_time (including absent)
                .OrderByDescending(a => a.entry_time)
                .Take(100) // Last 100 records
                .ToListAsync();
        }
        else if (isHRAdmin)
        {
            // HR Admin sees all attendance records (similar to System Admin)
            records = await _context.Attendance
                .Include(a => a.employee)
                .Include(a => a.shift)
                .Include(a => a.exception)
                .Where(a => a.entry_time.HasValue) // Include all records with entry_time (including absent)
                .OrderByDescending(a => a.entry_time)
                .Take(100) // Last 100 records
                .ToListAsync();
        }
        else if (isManager)
        {
            if (myAttendance == true)
            {
                // Manager wants to see only their own attendance
                records = await _context.Attendance
                    .Include(a => a.employee)
                    .Include(a => a.shift)
                    .Include(a => a.exception)
                    .Where(a => a.employee_id == employeeId && a.entry_time.HasValue)
                    .OrderByDescending(a => a.entry_time)
                    .Take(30) // Last 30 records
                    .ToListAsync();
            }
            else
            {
                // Managers see their team's attendance (default)
                var teamEmployeeIds = await _context.Employee
                    .Where(e => e.manager_id == employeeId)
                    .Select(e => e.employee_id)
                    .ToListAsync();
                
                teamEmployeeIds.Add(employeeId); // Include manager's own attendance
                
                records = await _context.Attendance
                    .Include(a => a.employee)
                    .Include(a => a.shift)
                    .Include(a => a.exception)
                    .Where(a => teamEmployeeIds.Contains(a.employee_id) && a.entry_time.HasValue)
                    .OrderByDescending(a => a.entry_time)
                    .ToListAsync();
            }
        }
        else
        {
            // Regular employees see only their own attendance
            records = await _context.Attendance
                .Include(a => a.employee)
                .Include(a => a.shift)
                .Include(a => a.exception)
                .Where(a => a.employee_id == employeeId && a.entry_time.HasValue)
                .OrderByDescending(a => a.entry_time)
                .Take(30) // Last 30 records
                .ToListAsync();
        }

        // Get pending shifts (assigned shifts that haven't been clocked in yet)
        // If manager is viewing only their own attendance, show only their pending shifts
        var showOnlyMyShifts = isManager && myAttendance == true;
        var pendingShifts = await GetPendingShiftsAsync(employeeId, isSystemAdmin, isHRAdmin, isManager && !showOnlyMyShifts);
        
        // Check for absences and mark them (only for current user's view scope to avoid excessive processing)
        // This will check absences for the employees visible in the current view
        var scopeEmployeeIds = isSystemAdmin || isHRAdmin 
            ? await _context.Employee.Where(e => e.is_active == true || e.is_active == null).Select(e => e.employee_id).ToListAsync()
            : isManager && !showOnlyMyShifts
                ? (await _context.Employee.Where(e => e.manager_id == employeeId).Select(e => e.employee_id).ToListAsync()).Concat(new[] { employeeId }).ToList()
                : new List<int> { employeeId };
        
        await CheckAndMarkAbsencesAsync(scopeEmployeeIds);

        ViewBag.IsManager = isManager;
        ViewBag.IsSystemAdmin = isSystemAdmin;
        ViewBag.IsHRAdmin = isHRAdmin;
        ViewBag.CurrentEmployeeId = employeeId;
        ViewBag.PendingShifts = pendingShifts;
        ViewBag.ShowOnlyMyAttendance = showOnlyMyShifts;
        return View(records);
    }

    private async Task<bool> IsEmployeeOnLeaveAsync(int employeeId, DateTime date)
    {
        // Check if there's a leave sync attendance record for this specific date
        // The SyncLeaveWithAttendance method creates records with login_method = "Leave Sync"
        // for each day the employee is on leave
        var leaveAttendance = await _context.Attendance
            .AnyAsync(a => a.employee_id == employeeId &&
                          a.login_method == "Leave Sync" &&
                          a.entry_time.HasValue &&
                          a.entry_time.Value.Date == date);

        if (leaveAttendance)
            return true;

        // Also check if there's an approved leave request that hasn't been synced yet
        // but would cover this date. Since LeaveRequest only has duration (not start/end dates),
        // we check if there's an approved leave that could cover today.
        // Note: This is a fallback - ideally leaves should be synced first
        var approvedLeaves = await _context.LeaveRequest
            .Where(lr => lr.employee_id == employeeId && 
                        lr.status == "Approved")
            .ToListAsync();

        // If there's an approved leave and no regular attendance for today,
        // assume they might be on leave (but this is less reliable)
        if (approvedLeaves.Any())
        {
            // Check if there's any attendance record for today (regular or leave)
            var anyAttendanceToday = await _context.Attendance
                .AnyAsync(a => a.employee_id == employeeId &&
                              a.entry_time.HasValue &&
                              a.entry_time.Value.Date == date);
            
            // If no attendance at all and there's an approved leave, they might be on leave
            // But we'll be conservative and only return true if we have a leave sync record
        }

        return false;
    }

    private async Task<List<object>> GetPendingShiftsAsync(int employeeId, bool isSystemAdmin, bool isHRAdmin, bool isManager)
    {
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;
        var pendingShifts = new List<object>();

        List<int> employeeIds;
        if (isSystemAdmin || isHRAdmin)
        {
            // Admins see all employees
            employeeIds = await _context.Employee
                .Where(e => e.is_active == true || e.is_active == null)
                .Select(e => e.employee_id)
                .ToListAsync();
        }
        else if (isManager)
        {
            // Managers see their team
            var teamIds = await _context.Employee
                .Where(e => e.manager_id == employeeId)
                .Select(e => e.employee_id)
                .ToListAsync();
            teamIds.Add(employeeId);
            employeeIds = teamIds;
        }
        else
        {
            // Regular employees see only their own
            employeeIds = new List<int> { employeeId };
        }

        // Get active shift assignments for today
        var activeAssignments = await _context.ShiftAssignment
            .Include(sa => sa.shift)
            .Include(sa => sa.employee)
            .Where(sa => employeeIds.Contains(sa.employee_id) &&
                        sa.status == "Active" &&
                        (!sa.start_date.HasValue || sa.start_date.Value <= DateOnly.FromDateTime(today)) &&
                        (!sa.end_date.HasValue || sa.end_date.Value >= DateOnly.FromDateTime(today)))
            .ToListAsync();

        foreach (var assignment in activeAssignments)
        {
            if (assignment.shift == null || !assignment.shift.start_time.HasValue) continue;

            var shiftStartTime = today.Add(assignment.shift.start_time.Value.ToTimeSpan());
            var shiftEndTime = assignment.shift.end_time.HasValue 
                ? today.Add(assignment.shift.end_time.Value.ToTimeSpan())
                : (DateTime?)null;

            // Check if employee is on leave
            var isOnLeave = await IsEmployeeOnLeaveAsync(assignment.employee_id, today);

            // Check if there's already an attendance record for this shift today
            var existingAttendance = await _context.Attendance
                .FirstOrDefaultAsync(a => a.employee_id == assignment.employee_id &&
                                         a.shift_id == assignment.shift_id &&
                                         a.entry_time.HasValue &&
                                         a.entry_time.Value.Date == today);

            // Only show pending shifts that haven't ended yet, or are currently active
            // If shift has ended and employee clocked in, it will appear in attendance records
            var shiftHasEnded = shiftEndTime.HasValue && now > shiftEndTime.Value;
            
            // Skip if shift has ended and employee has clocked in (it's in attendance records now)
            if (shiftHasEnded && existingAttendance != null && existingAttendance.exit_time.HasValue)
            {
                continue; // This shift is complete, it's in attendance records
            }

            // Check if employee is on leave
            var status = isOnLeave ? "On Leave" : "Pending";
            var canClockIn = false;
            var canClockOut = false;
            var attendanceId = 0;
            
            if (isOnLeave)
            {
                // Employee is on leave - show as "On Leave" regardless of time
                pendingShifts.Add(new
                {
                    EmployeeId = assignment.employee_id,
                    EmployeeName = assignment.employee?.full_name ?? assignment.employee?.email ?? "Unknown",
                    ShiftId = assignment.shift_id,
                    ShiftName = assignment.shift.name,
                    StartTime = shiftStartTime,
                    EndTime = shiftEndTime,
                    Status = status,
                    AssignmentId = assignment.assignment_id,
                    CanClockIn = false,
                    CanClockOut = false,
                    AttendanceId = 0
                });
            }
            else
            {
                // Check if shift has started
                if (now >= shiftStartTime)
                {
                    if (shiftHasEnded)
                    {
                        // Shift has ended
                        if (existingAttendance == null)
                        {
                            status = "Absent";
                        }
                        else if (existingAttendance.exit_time.HasValue)
                        {
                            // Shift completed, don't show in pending (will be in attendance records)
                            continue;
                        }
                        else
                        {
                            // Shift ended but employee hasn't clocked out yet
                            status = "Shift Ended - Clock Out Required";
                            canClockOut = true;
                            attendanceId = existingAttendance.attendance_id;
                        }
                    }
                    else
                    {
                        // Shift is in progress
                        if (existingAttendance == null)
                        {
                            status = "Started - Not Clocked In";
                            canClockIn = true;
                        }
                        else if (existingAttendance.exit_time.HasValue)
                        {
                            // Already clocked out, should be in attendance records
                            continue;
                        }
                        else
                        {
                            // Clocked in but not clocked out yet
                            status = "Active - Clocked In";
                            canClockOut = true;
                            attendanceId = existingAttendance.attendance_id;
                        }
                    }
                }
                else
                {
                    // Shift hasn't started yet
                    status = "Pending";
                }

                pendingShifts.Add(new
                {
                    EmployeeId = assignment.employee_id,
                    EmployeeName = assignment.employee?.full_name ?? assignment.employee?.email ?? "Unknown",
                    ShiftId = assignment.shift_id,
                    ShiftName = assignment.shift.name,
                    StartTime = shiftStartTime,
                    EndTime = shiftEndTime,
                    Status = status,
                    AssignmentId = assignment.assignment_id,
                    CanClockIn = canClockIn,
                    CanClockOut = canClockOut,
                    AttendanceId = attendanceId
                });
            }
        }

        return pendingShifts;
    }

    private async Task CheckAndMarkAbsencesAsync(List<int>? employeeIds = null)
    {
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Get all active shift assignments (optionally filtered by employee IDs)
        var query = _context.ShiftAssignment
            .Include(sa => sa.shift)
            .Include(sa => sa.employee)
            .Where(sa => sa.status == "Active" &&
                        (!sa.start_date.HasValue || sa.start_date.Value <= DateOnly.FromDateTime(today)) &&
                        (!sa.end_date.HasValue || sa.end_date.Value >= DateOnly.FromDateTime(today)) &&
                        sa.shift != null &&
                        sa.shift.end_time.HasValue);

        // Filter by employee IDs if provided (to limit scope)
        if (employeeIds != null && employeeIds.Any())
        {
            query = query.Where(sa => employeeIds.Contains(sa.employee_id));
        }

        var activeAssignments = await query.ToListAsync();

        foreach (var assignment in activeAssignments)
        {
            if (assignment.shift == null || !assignment.shift.end_time.HasValue) continue;

            var shiftEndTime = today.Add(assignment.shift.end_time.Value.ToTimeSpan());

            // Only check if shift has ended
            if (now <= shiftEndTime) continue;

            // Check if employee is on leave - don't mark as absent if on leave
            var isOnLeave = await IsEmployeeOnLeaveAsync(assignment.employee_id, today);
            if (isOnLeave)
            {
                _logger.LogInformation("Skipping absence check for employee {EmployeeId} on {Date} - employee is on leave",
                    assignment.employee_id, today);
                continue;
            }

            // Check if employee clocked in for this shift today
            var attendance = await _context.Attendance
                .FirstOrDefaultAsync(a => a.employee_id == assignment.employee_id &&
                                         a.shift_id == assignment.shift_id &&
                                         a.entry_time.HasValue &&
                                         a.entry_time.Value.Date == today);

            // Also check if there's already an absent record
            var existingAbsent = await _context.Attendance
                .FirstOrDefaultAsync(a => a.employee_id == assignment.employee_id &&
                                         a.shift_id == assignment.shift_id &&
                                         a.login_method == "Absent" &&
                                         a.entry_time.HasValue &&
                                         a.entry_time.Value.Date == today);

            if (attendance == null && existingAbsent == null)
            {
                // Mark as absent
                var absentAttendance = new Attendance
                {
                    employee_id = assignment.employee_id,
                    shift_id = assignment.shift_id,
                    entry_time = today, // Use today's date for entry_time to allow proper querying
                    exit_time = null,
                    login_method = "Absent",
                    logout_method = "Absent",
                    duration = 0
                };

                absentAttendance.exception_id = await GetOrCreateException("Absent");

                _context.Attendance.Add(absentAttendance);
                await _context.SaveChangesAsync();

                // Create notification for absence
                await _notificationService.CreateNotificationAsync(
                    assignment.employee_id,
                    "Absence Recorded",
                    $"You were marked as absent for shift '{assignment.shift.name}' on {today:MMM dd, yyyy}.",
                    "Attendance",
                    "High"
                );

                _logger.LogInformation("Marked employee {EmployeeId} as absent for shift {ShiftId} on {Date}",
                    assignment.employee_id, assignment.shift_id, today);
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> TeamAttendance(int? days = 90)
    {
        if (!IsManager())
        {
            TempData["ErrorMessage"] = "You don't have permission to view team attendance.";
            return RedirectToAction("Index");
        }

        var managerId = GetCurrentEmployeeId();
        if (managerId == 0)
            return RedirectToAction("Login", "Account");

        // Get all team members (including manager)
        var teamMembers = await _context.Employee
            .Where(e => e.manager_id == managerId && (e.is_active == true || e.is_active == null))
            .OrderBy(e => e.full_name)
            .ToListAsync();

        var teamEmployeeIds = teamMembers.Select(e => e.employee_id).ToList();
        teamEmployeeIds.Add(managerId); // Include manager's own attendance

        // Get attendance records for the specified period
        var startDate = DateTime.UtcNow.AddDays(-(days ?? 90));
        var records = await _context.Attendance
            .Include(a => a.employee)
            .Include(a => a.exception)
            .Include(a => a.shift)
            .Where(a => teamEmployeeIds.Contains(a.employee_id) && 
                       a.entry_time.HasValue && 
                       a.entry_time.Value >= startDate)
            .OrderByDescending(a => a.entry_time)
            .ToListAsync();

        // Get team member names for filter dropdown
        var allTeamMembers = teamMembers.ToList();
        var manager = await _context.Employee.FindAsync(managerId);
        if (manager != null && !allTeamMembers.Any(t => t.employee_id == managerId))
        {
            allTeamMembers.Insert(0, manager);
        }

        ViewBag.TeamMembers = allTeamMembers;
        ViewBag.Days = days ?? 90;
        ViewBag.TotalRecords = records.Count;
        ViewBag.TotalTeamSize = allTeamMembers.Count;
        
        return View(records);
    }

    public IActionResult Record()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
            return RedirectToAction("Login", "Account");

        // Check if already clocked in today
        var today = DateTime.UtcNow.Date;
        var existingAttendance = _context.Attendance
            .FirstOrDefault(a => a.employee_id == employeeId && 
                                 a.entry_time.HasValue && 
                                 a.entry_time.Value.Date == today &&
                                 !a.exit_time.HasValue);

        ViewBag.HasActiveAttendance = existingAttendance != null;
        ViewBag.CurrentAttendance = existingAttendance;
        ViewBag.CurrentEmployeeId = employeeId;
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ClockIn()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
            return Json(new { success = false, message = "Not authenticated" });

        var now = DateTime.UtcNow;
        var today = now.Date;

        // Check if already clocked in today
        var existing = await _context.Attendance
            .FirstOrDefaultAsync(a => a.employee_id == employeeId && 
                                     a.entry_time.HasValue && 
                                     a.entry_time.Value.Date == today &&
                                     !a.exit_time.HasValue);

        if (existing != null)
        {
            return Json(new { success = false, message = "You are already clocked in today" });
        }

        // Get employee's shift
        var shiftAssignment = await _context.ShiftAssignment
            .Include(s => s.shift)
            .Where(s => s.employee_id == employeeId && s.status == "Active")
            .OrderByDescending(s => s.assigned_at)
            .FirstOrDefaultAsync();

        var attendance = new Attendance
        {
            employee_id = employeeId,
            entry_time = now,
            login_method = "Web",
            shift_id = shiftAssignment?.shift_id
        };

        // Apply grace period and check for lateness
        if (shiftAssignment?.shift != null && shiftAssignment.shift.start_time.HasValue)
        {
            var latenessPolicy = await _context.LatenessPolicy
                .Include(l => l.policy)
                .FirstOrDefaultAsync();

            var gracePeriod = latenessPolicy?.grace_period_mins ?? 15; // Default 15 minutes
            var shiftStartTime = shiftAssignment.shift.start_time.Value;
            var expectedTime = today.Add(shiftStartTime.ToTimeSpan());
            
            // Calculate how late they are (without grace period)
            var lateBy = now - expectedTime;

            // Only mark as late if they're more than grace period minutes late
            if (lateBy.TotalMinutes > gracePeriod)
            {
                attendance.exception_id = await GetOrCreateException("Late Arrival");
                
                // Calculate actual late minutes (after grace period)
                var lateMinutes = (int)(lateBy.TotalMinutes - gracePeriod);
                
                // Apply short-time penalty (deduction rate from lateness policy)
                if (latenessPolicy?.deduction_rate.HasValue == true && latenessPolicy.deduction_rate.Value > 0)
                {
                    var penaltyRate = latenessPolicy.deduction_rate.Value; // e.g., 0.05 = 5%
                    
                    // Apply penalty to current payroll period if exists
                    var currentPayroll = await _context.Payroll
                        .Where(p => p.employee_id == employeeId && 
                                   p.period_start <= DateOnly.FromDateTime(today) &&
                                   p.period_end >= DateOnly.FromDateTime(today))
                        .FirstOrDefaultAsync();
                    
                    if (currentPayroll != null)
                    {
                        // Calculate penalty amount based on late minutes (after grace period)
                        var penaltyAmount = (currentPayroll.base_amount ?? 0) * penaltyRate * (lateMinutes / 60.0m);
                        currentPayroll.adjustments = (currentPayroll.adjustments ?? 0) - penaltyAmount;
                        
                        // Create allowance deduction record
                        var deduction = new AllowanceDeduction
                        {
                            employee_id = employeeId,
                            payroll_id = currentPayroll.payroll_id,
                            type = "Deduction",
                            amount = penaltyAmount,
                            duration = $"{lateMinutes} minutes late (after {gracePeriod} min grace period)",
                            timezone = "UTC"
                        };
                        _context.AllowanceDeduction.Add(deduction);
                        
                        // Create notification for penalty
                        await _notificationService.CreateNotificationAsync(
                            employeeId,
                            "Lateness Penalty Applied",
                            $"You were {lateMinutes} minutes late (after {gracePeriod} min grace period) for shift '{shiftAssignment.shift.name}' on {today:MMM dd, yyyy}. A penalty of {penaltyAmount:F2} has been applied to your payroll.",
                            "Attendance",
                            "High"
                        );
                        
                        _logger.LogInformation("Applied lateness penalty of {PenaltyAmount} for employee {EmployeeId} ({LateMinutes} minutes late after grace period)", 
                            penaltyAmount, employeeId, lateMinutes);
                    }
                }
            }
            else if (lateBy.TotalMinutes > 0 && lateBy.TotalMinutes <= gracePeriod)
            {
                // Within grace period - no penalty, no late mark
                _logger.LogInformation("Employee {EmployeeId} clocked in {LateMinutes:F1} minutes late but within {GracePeriod} minute grace period - no penalty applied",
                    employeeId, lateBy.TotalMinutes, gracePeriod);
            }
        }

        _context.Attendance.Add(attendance);
        await _context.SaveChangesAsync();

        // Log the action
        var log = new AttendanceLog
        {
            attendance_id = attendance.attendance_id,
            actor = employeeId,
            timestamp = now,
            reason = "Clock In - Web"
        };
        _context.AttendanceLog.Add(log);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Clocked in successfully", attendanceId = attendance.attendance_id });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutRequest request)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
            return Json(new { success = false, message = "Not authenticated" });

        if (request == null || request.AttendanceId == 0)
            return Json(new { success = false, message = "Invalid request" });

        var attendance = await _context.Attendance
            .Include(a => a.shift)
            .FirstOrDefaultAsync(a => a.attendance_id == request.AttendanceId && a.employee_id == employeeId);

        if (attendance == null)
            return Json(new { success = false, message = "Attendance record not found" });

        if (attendance.exit_time.HasValue)
            return Json(new { success = false, message = "Already clocked out" });

        var now = DateTime.UtcNow;
        var today = now.Date;
        attendance.exit_time = now;
        attendance.logout_method = "Web";

        if (attendance.entry_time.HasValue)
        {
            attendance.duration = (decimal)(now - attendance.entry_time.Value).TotalHours;
            
            // Check for short-time penalty (early clock out)
            if (attendance.shift != null && attendance.shift.end_time.HasValue)
            {
                var shiftEndTime = attendance.shift.end_time.Value;
                var expectedEndTime = today.Add(shiftEndTime.ToTimeSpan());
                
                // Check if clocking out early (before expected end time)
                if (now < expectedEndTime)
                {
                    var earlyBy = expectedEndTime - now;
                    
                    // Apply short-time penalty if leaving more than 15 minutes early
                    if (earlyBy.TotalMinutes > 15)
                    {
                        // Get lateness policy for penalty rates
                        var latenessPolicy = await _context.LatenessPolicy
                            .Include(l => l.policy)
                            .FirstOrDefaultAsync();

                        if (latenessPolicy?.deduction_rate.HasValue == true && latenessPolicy.deduction_rate.Value > 0)
                        {
                            var earlyMinutes = (int)earlyBy.TotalMinutes;
                            var penaltyRate = latenessPolicy.deduction_rate.Value;
                            decimal penaltyAmount = 0;
                            
                            // Apply penalty to current payroll period if exists
                            var currentPayroll = await _context.Payroll
                                .Where(p => p.employee_id == employeeId && 
                                           p.period_start <= DateOnly.FromDateTime(today) &&
                                           p.period_end >= DateOnly.FromDateTime(today))
                                .FirstOrDefaultAsync();
                            
                            if (currentPayroll != null)
                            {
                                // Calculate penalty amount based on early minutes
                                penaltyAmount = (currentPayroll.base_amount ?? 0) * penaltyRate * (earlyMinutes / 60.0m);
                                currentPayroll.adjustments = (currentPayroll.adjustments ?? 0) - penaltyAmount;
                                
                                // Create allowance deduction record
                                var deduction = new AllowanceDeduction
                                {
                                    employee_id = employeeId,
                                    payroll_id = currentPayroll.payroll_id,
                                    type = "Deduction",
                                    amount = penaltyAmount,
                                    duration = $"{earlyMinutes} minutes early",
                                    timezone = "UTC"
                                };
                                _context.AllowanceDeduction.Add(deduction);
                                
                                _logger.LogInformation("Applied short-time penalty of {PenaltyAmount} for employee {EmployeeId} (left {EarlyMinutes} minutes early)", 
                                    penaltyAmount, employeeId, earlyMinutes);
                            }
                            
                            // Create exception for early departure
                            attendance.exception_id = await GetOrCreateException("Early Departure");
                            
                            // Create notification for early departure penalty
                            await _notificationService.CreateNotificationAsync(
                                employeeId,
                                "Early Departure Penalty Applied",
                                $"You clocked out {earlyMinutes} minutes early from shift '{attendance.shift?.name ?? "your shift"}' on {today:MMM dd, yyyy}. A penalty of {penaltyAmount:F2} has been applied to your payroll.",
                                "Attendance",
                                "High"
                            );
                        }
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        // Log the action
        var log = new AttendanceLog
        {
            attendance_id = attendance.attendance_id,
            actor = employeeId,
            timestamp = now,
            reason = "Clock Out - Web"
        };
        _context.AttendanceLog.Add(log);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Clocked out successfully" });
    }

    private async Task<int?> GetOrCreateException(string exceptionName)
    {
        var exception = await _context.Exception
            .FirstOrDefaultAsync(e => e.name == exceptionName);

        if (exception == null)
        {
            exception = new HRMS.Models.Exception
            {
                name = exceptionName,
                category = "Attendance",
                status = "Active"
            };
            _context.Exception.Add(exception);
            await _context.SaveChangesAsync();
        }

        return exception.exception_id;
    }
    public IActionResult Correction()
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
            return RedirectToAction("Login", "Account");

        // Get recent attendance records for the employee
        var recentAttendance = _context.Attendance
            .Where(a => a.employee_id == employeeId)
            .OrderByDescending(a => a.entry_time)
            .Take(10)
            .ToList();

        ViewBag.RecentAttendance = recentAttendance;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Correction(AttendanceCorrectionRequest request)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
            return RedirectToAction("Login", "Account");

        request.employee_id = employeeId;
        request.status = "Pending";
        request.recorded_by = employeeId;

        _context.AttendanceCorrectionRequest.Add(request);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Correction request submitted successfully. It will be reviewed by your manager.";
        return RedirectToAction("Correction");
    }
    public async Task<IActionResult> TeamSummary(int? days = 30, int? employeeId = null)
    {
        if (!IsManager())
        {
            TempData["ErrorMessage"] = "You don't have permission to view team summaries.";
        return RedirectToAction("Index");
        }

        var managerId = GetCurrentEmployeeId();
        var teamMembers = await _context.Employee
            .Where(e => e.manager_id == managerId && (e.is_active == true || e.is_active == null))
            .OrderBy(e => e.full_name)
            .ToListAsync();

        if (!teamMembers.Any())
        {
            ViewBag.TeamMembers = new List<Employee>();
            ViewBag.Summary = new List<object>();
            ViewBag.Days = days ?? 30;
            ViewBag.SelectedEmployeeId = employeeId;
            ViewBag.PendingCorrections = 0;
            return View();
        }

        var teamEmployeeIds = teamMembers.Select(e => e.employee_id).ToList();
        teamEmployeeIds.Add(managerId);

        // Filter by selected employee if provided
        if (employeeId.HasValue && teamEmployeeIds.Contains(employeeId.Value))
        {
            teamEmployeeIds = new List<int> { employeeId.Value };
        }

        var startDate = DateTime.UtcNow.AddDays(-(days ?? 30));
        var attendanceData = await _context.Attendance
            .Include(a => a.employee)
            .Include(a => a.exception)
            .Where(a => teamEmployeeIds.Contains(a.employee_id) && 
                       a.entry_time.HasValue && 
                       a.entry_time.Value >= startDate)
            .OrderByDescending(a => a.entry_time)
            .ToListAsync();

        // Include manager in team members list if not already there
        var allTeamMembers = teamMembers.ToList();
        if (!allTeamMembers.Any(t => t.employee_id == managerId))
        {
            var manager = await _context.Employee.FindAsync(managerId);
            if (manager != null)
            {
                allTeamMembers.Insert(0, manager);
            }
        }

        // Calculate summary statistics for all team members (including manager)
        var summary = new List<object>();
        
        // Process all team members (which now includes manager if not already there)
        foreach (var member in allTeamMembers)
        {
            var memberAttendance = attendanceData.Where(a => a.employee_id == member.employee_id).ToList();
            
            summary.Add(new
            {
                EmployeeId = member.employee_id,
                EmployeeName = member.full_name ?? member.email ?? "Unknown",
                EmployeeEmail = member.email ?? "",
                TotalDays = memberAttendance.Count,
                TotalHours = memberAttendance.Sum(a => a.duration ?? 0),
                LateCount = memberAttendance.Count(a => a.exception_id != null),
                AverageHours = memberAttendance.Any() ? memberAttendance.Average(a => a.duration ?? 0) : 0,
                RecentRecords = memberAttendance.OrderByDescending(a => a.entry_time).Take(5).ToList()
            });
        }

        // Get pending correction requests for team (update teamEmployeeIds to include manager)
        var allTeamEmployeeIds = allTeamMembers.Select(e => e.employee_id).ToList();
        var pendingCorrections = await _context.AttendanceCorrectionRequest
            .Include(c => c.employee)
            .Where(c => allTeamEmployeeIds.Contains(c.employee_id) && c.status == "Pending")
            .CountAsync();

        ViewBag.TeamMembers = allTeamMembers;
        ViewBag.Summary = summary;
        ViewBag.Days = days ?? 30;
        ViewBag.SelectedEmployeeId = employeeId;
        ViewBag.PendingCorrections = pendingCorrections;
        ViewBag.AttendanceData = attendanceData.OrderByDescending(a => a.entry_time).Take(100).ToList(); // Recent records for detailed view
        ViewBag.TotalTeamSize = allTeamMembers.Count;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> AdminSync()
    {
        if (!await IsSystemAdminAsync())
        {
            TempData["ErrorMessage"] = "You don't have permission to access this feature.";
            return RedirectToAction("Index");
        }

        // Get statistics about pending leave requests
        var pendingLeaves = await _context.LeaveRequest
            .Include(l => l.employee)
            .Where(l => l.status == "Approved")
            .CountAsync();

        var alreadySynced = await _context.Attendance
            .Where(a => a.login_method == "Leave Sync")
            .CountAsync();

        ViewBag.PendingLeaves = pendingLeaves;
        ViewBag.AlreadySynced = alreadySynced;
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SyncLeaveWithAttendance()
    {
        if (!await IsSystemAdminAsync())
        {
            return Json(new { success = false, message = "You don't have permission to perform this action." });
        }

        try
        {
            // Get all approved leave requests that haven't been synced yet
            var approvedLeaves = await _context.LeaveRequest
                .Include(l => l.employee)
                .Include(l => l.leave)
                .Where(l => l.status == "Approved" && l.employee.is_active == true)
                .ToListAsync();

            if (!approvedLeaves.Any())
            {
                return Json(new { success = true, message = "No approved leave requests found to sync.", syncedCount = 0, skippedCount = 0 });
            }

            var syncedCount = 0;
            var skippedCount = 0;
            var shiftsMarkedCount = 0;
            var today = DateTime.UtcNow.Date;

            foreach (var leaveRequest in approvedLeaves)
            {
                // Check if this leave request has already been synced
                var alreadySynced = await _context.Attendance
                    .AnyAsync(a => a.employee_id == leaveRequest.employee_id &&
                                 a.login_method == "Leave Sync" &&
                                 a.entry_time.HasValue &&
                                 a.entry_time.Value.Date >= today.AddDays(-leaveRequest.duration));

                if (alreadySynced)
                {
                    skippedCount++;
                    continue;
                }

                // Get employee's active shift assignments
                var activeShifts = await _context.ShiftAssignment
                    .Include(sa => sa.shift)
                    .Where(sa => sa.employee_id == leaveRequest.employee_id &&
                                sa.status == "Active")
                    .ToListAsync();

                // Create attendance records for the leave duration
                // We'll create records going back from today based on duration
                for (int i = 0; i < leaveRequest.duration && i < 90; i++) // Max 90 days back
                {
                    var date = today.AddDays(-i);
                    
                    // Check if employee has an assigned shift for this date
                    var shiftForDate = activeShifts.FirstOrDefault(sa =>
                        (!sa.start_date.HasValue || sa.start_date.Value <= DateOnly.FromDateTime(date)) &&
                        (!sa.end_date.HasValue || sa.end_date.Value >= DateOnly.FromDateTime(date)));

                    var existingAttendance = await _context.Attendance
                        .FirstOrDefaultAsync(a => a.employee_id == leaveRequest.employee_id &&
                                                 a.entry_time.HasValue &&
                                                 a.entry_time.Value.Date == date);

                    if (existingAttendance == null)
                    {
                        // Create leave attendance record
                        var attendance = new Attendance
                        {
                            employee_id = leaveRequest.employee_id,
                            entry_time = date.AddHours(9), // Standard work start
                            exit_time = date.AddHours(17), // Standard work end
                            duration = 8, // 8 hours
                            login_method = "Leave Sync",
                            logout_method = "Leave Sync",
                            shift_id = shiftForDate?.shift_id
                        };
                        _context.Attendance.Add(attendance);
                        syncedCount++;
                        
                        // If there's an assigned shift for this date, mark it appropriately
                        // The IsEmployeeOnLeaveAsync will now detect this and prevent absence marking
                        if (shiftForDate != null)
                        {
                            shiftsMarkedCount++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            var message = $"Sync completed successfully! Synced {syncedCount} attendance record(s) from {approvedLeaves.Count} approved leave request(s).";
            if (skippedCount > 0)
            {
                message += $" {skippedCount} request(s) were skipped (already synced).";
            }
            if (shiftsMarkedCount > 0)
            {
                message += $" {shiftsMarkedCount} assigned shift(s) during leave period(s) are now marked as on leave.";
            }
            
            _logger.LogInformation("Admin synced {Count} leave records with attendance system. {SkippedCount} skipped. {ShiftsMarked} shifts marked.", 
                syncedCount, skippedCount, shiftsMarkedCount);
            
            return Json(new { 
                success = true, 
                message = message,
                syncedCount = syncedCount,
                skippedCount = skippedCount,
                shiftsMarkedCount = shiftsMarkedCount
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error syncing leave with attendance");
            return Json(new { success = false, message = $"An error occurred while syncing: {ex.Message}" });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SyncOfflineAttendance([FromBody] List<OfflineAttendanceRecord> offlineRecords)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
            return Json(new { success = false, message = "Not authenticated" });

        if (offlineRecords == null || !offlineRecords.Any())
            return Json(new { success = true, message = "No records to sync" });

        var syncedCount = 0;
        var errorCount = 0;

        foreach (var record in offlineRecords)
        {
            try
            {
                // Verify the record belongs to the current user
                if (record.EmployeeId != employeeId)
                {
                    errorCount++;
                    continue;
                }

                if (record.Action == "ClockIn")
                {
                    // Check if already clocked in for this date
                    var recordDate = DateTime.Parse(record.Timestamp).Date;
                    var existing = await _context.Attendance
                        .FirstOrDefaultAsync(a => a.employee_id == employeeId &&
                                                 a.entry_time.HasValue &&
                                                 a.entry_time.Value.Date == recordDate &&
                                                 !a.exit_time.HasValue);

                    if (existing == null)
                    {
                        // Get employee's shift
                        var shiftAssignment = await _context.ShiftAssignment
                            .Include(s => s.shift)
                            .Where(s => s.employee_id == employeeId && s.status == "Active")
                            .OrderByDescending(s => s.assigned_at)
                            .FirstOrDefaultAsync();

                        var attendance = new Attendance
                        {
                            employee_id = employeeId,
                            entry_time = DateTime.Parse(record.Timestamp),
                            login_method = "Offline Sync",
                            shift_id = shiftAssignment?.shift_id
                        };

                        // Apply grace period and lateness check if shift exists
                        if (shiftAssignment?.shift != null && shiftAssignment.shift.start_time.HasValue)
                        {
                            var latenessPolicy = await _context.LatenessPolicy
                                .Include(l => l.policy)
                                .FirstOrDefaultAsync();

                            var gracePeriod = latenessPolicy?.grace_period_mins ?? 15;
                            var shiftStartTime = shiftAssignment.shift.start_time.Value;
                            var expectedTime = recordDate.Add(shiftStartTime.ToTimeSpan());
                            var clockInTime = DateTime.Parse(record.Timestamp);
                            var lateBy = clockInTime - expectedTime;

                            if (lateBy.TotalMinutes > gracePeriod)
                            {
                                attendance.exception_id = await GetOrCreateException("Late Arrival");
                            }
                        }

                        _context.Attendance.Add(attendance);
                        await _context.SaveChangesAsync();

                        // Log the sync
                        var log = new AttendanceLog
                        {
                            attendance_id = attendance.attendance_id,
                            actor = employeeId,
                            timestamp = DateTime.UtcNow,
                            reason = "Offline Sync - Clock In"
                        };
                        _context.AttendanceLog.Add(log);
                        syncedCount++;
                    }
                }
                else if (record.Action == "ClockOut")
                {
                    // Find the attendance record to clock out
                    var attendance = await _context.Attendance
                        .FirstOrDefaultAsync(a => a.attendance_id == record.AttendanceId &&
                                                 a.employee_id == employeeId &&
                                                 !a.exit_time.HasValue);

                    if (attendance != null)
                    {
                        var clockOutTime = DateTime.Parse(record.Timestamp);
                        attendance.exit_time = clockOutTime;
                        attendance.logout_method = "Offline Sync";

                        if (attendance.entry_time.HasValue)
                        {
                            attendance.duration = (decimal)(clockOutTime - attendance.entry_time.Value).TotalHours;
                        }

                        // Check for early departure penalty
                        if (attendance.shift != null && attendance.shift.end_time.HasValue)
                        {
                            var shiftEndTime = attendance.shift.end_time.Value;
                            var expectedEndTime = attendance.entry_time.Value.Date.Add(shiftEndTime.ToTimeSpan());
                            
                            if (clockOutTime < expectedEndTime)
                            {
                                var earlyBy = expectedEndTime - clockOutTime;
                                if (earlyBy.TotalMinutes > 15)
                                {
                                    var latenessPolicy = await _context.LatenessPolicy
                                        .Include(l => l.policy)
                                        .FirstOrDefaultAsync();

                                    if (latenessPolicy?.deduction_rate.HasValue == true && latenessPolicy.deduction_rate.Value > 0)
                                    {
                                        var earlyMinutes = (int)earlyBy.TotalMinutes;
                                        var penaltyRate = latenessPolicy.deduction_rate.Value;
                                        var today = clockOutTime.Date;
                                        
                                        var currentPayroll = await _context.Payroll
                                            .Where(p => p.employee_id == employeeId &&
                                                       p.period_start <= DateOnly.FromDateTime(today) &&
                                                       p.period_end >= DateOnly.FromDateTime(today))
                                            .FirstOrDefaultAsync();
                                        
                                        if (currentPayroll != null)
                                        {
                                            var penaltyAmount = (currentPayroll.base_amount ?? 0) * penaltyRate * (earlyMinutes / 60.0m);
                                            currentPayroll.adjustments = (currentPayroll.adjustments ?? 0) - penaltyAmount;
                                            
                                            var deduction = new AllowanceDeduction
                                            {
                                                employee_id = employeeId,
                                                payroll_id = currentPayroll.payroll_id,
                                                type = "Deduction",
                                                amount = penaltyAmount,
                                                duration = $"{earlyMinutes} minutes early",
                                                timezone = "UTC"
                                            };
                                            _context.AllowanceDeduction.Add(deduction);
                                            
                                            attendance.exception_id = await GetOrCreateException("Early Departure");
                                        }
                                    }
                                }
                            }
                        }

                        await _context.SaveChangesAsync();

                        // Log the sync
                        var log = new AttendanceLog
                        {
                            attendance_id = attendance.attendance_id,
                            actor = employeeId,
                            timestamp = DateTime.UtcNow,
                            reason = "Offline Sync - Clock Out"
                        };
                        _context.AttendanceLog.Add(log);
                        syncedCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error syncing offline record: {Record}", System.Text.Json.JsonSerializer.Serialize(record));
                errorCount++;
            }
        }

        var message = $"Synced {syncedCount} offline attendance record(s)";
        if (errorCount > 0)
        {
            message += $". {errorCount} record(s) failed to sync.";
        }

        return Json(new { success = true, message = message, syncedCount = syncedCount, errorCount = errorCount });
    }

    // Helper class for offline attendance records
    public class OfflineAttendanceRecord
    {
        public int EmployeeId { get; set; }
        public string Action { get; set; } = ""; // "ClockIn" or "ClockOut"
        public string Timestamp { get; set; } = "";
        public int? AttendanceId { get; set; } // For ClockOut
    }

    public async Task<IActionResult> ApproveCorrection(int id)
    {
        if (!IsManager())
        {
            return RedirectToAction("Index");
        }

        var request = await _context.AttendanceCorrectionRequest
            .Include(r => r.employee)
            .FirstOrDefaultAsync(r => r.attendance_correction_request_id == id);

        if (request != null)
        {
            request.status = "Approved";
            
            // Apply the correction to attendance if needed
            if (request.correction_type == "Time Adjustment")
            {
                // Logic to update attendance record
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Correction request approved.";
        }
        return RedirectToAction("TeamSummary");
    }

    public async Task<IActionResult> RejectCorrection(int id)
    {
        if (!IsManager())
        {
            return RedirectToAction("Index");
        }

        var request = await _context.AttendanceCorrectionRequest.FindAsync(id);
        if (request != null)
        {
            request.status = "Rejected";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Correction request rejected.";
        }
        return RedirectToAction("TeamSummary");
    }
}  

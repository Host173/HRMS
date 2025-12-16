using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;
using HRMS.Helpers;

namespace HRMS.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly HrmsDbContext _context;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(HrmsDbContext context, ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var hrmsDbContext = _context.Employee.Include(e => e.contract).Include(e => e.department).Include(e => e.manager).Include(e => e.pay_grade).Include(e => e.position).Include(e => e.salary_type).Include(e => e.tax_form);
            return View(await hrmsDbContext.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee
                .Include(e => e.contract)
                .Include(e => e.department)
                .Include(e => e.manager)
                .Include(e => e.pay_grade)
                .Include(e => e.position)
                .Include(e => e.salary_type)
                .Include(e => e.tax_form)
                .FirstOrDefaultAsync(m => m.employee_id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Create
        public async Task<IActionResult> Create()
        {
            var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
            bool isHRAdmin = false;
            
            if (currentEmployeeId.HasValue)
            {
                isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
            }
            
            ViewData["contract_id"] = new SelectList(_context.Contract, "contract_id", "contract_id");
            ViewData["department_id"] = new SelectList(_context.Department, "department_id", "department_id");
            
            // Filter manager list based on role
            if (isHRAdmin)
            {
                // HR Admins can assign any employee as manager
                ViewData["manager_id"] = new SelectList(await _context.Employee.Where(e => e.is_active == true).ToListAsync(), "employee_id", "full_name");
            }
            else
            {
                // Non-HR users should only see employees who can be managed (excluding Managers, HR Admins, and System Admins)
                var assignableEmployees = await AuthorizationHelper.GetAssignableEmployeesAsync(_context, null);
                ViewData["manager_id"] = new SelectList(assignableEmployees, "employee_id", "full_name");
            }
            
            ViewData["pay_grade_id"] = new SelectList(_context.PayGrade, "pay_grade_id", "pay_grade_id");
            ViewData["position_id"] = new SelectList(_context.Position, "position_id", "position_id");
            ViewData["salary_type_id"] = new SelectList(_context.SalaryType, "salary_type_id", "salary_type_id");
            ViewData["tax_form_id"] = new SelectList(_context.TaxForm, "tax_form_id", "tax_form_id");
            return View();
        }

        // POST: Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("employee_id,first_name,last_name,full_name,national_id,date_of_birth,country_of_birth,phone,email,address,emergency_contact_name,emergency_contact_phone,relationship,biography,profile_image,employment_progress,account_status,employment_status,hire_date,is_active,profile_completion,department_id,position_id,manager_id,contract_id,tax_form_id,salary_type_id,pay_grade_id,password_hash")] Employee employee)
        {
            var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
            bool isHRAdmin = false;
            
            if (currentEmployeeId.HasValue)
            {
                isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
            }
            
            // Validate manager assignment if manager_id is provided
            if (!isHRAdmin && employee.manager_id.HasValue)
            {
                var canBeManaged = await AuthorizationHelper.CanBeManagedByLineManagerAsync(_context, employee.manager_id.Value);
                if (!canBeManaged)
                {
                    ModelState.AddModelError("manager_id", "You cannot assign employees with Manager, HR Admin, or System Admin roles. Only HR Admins can manage such assignments.");
                }
            }
            
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["contract_id"] = new SelectList(_context.Contract, "contract_id", "contract_id", employee.contract_id);
            ViewData["department_id"] = new SelectList(_context.Department, "department_id", "department_id", employee.department_id);
            
            // Filter manager list based on role
            if (isHRAdmin)
            {
                ViewData["manager_id"] = new SelectList(await _context.Employee.Where(e => e.is_active == true).ToListAsync(), "employee_id", "full_name", employee.manager_id);
            }
            else
            {
                var assignableEmployees = await AuthorizationHelper.GetAssignableEmployeesAsync(_context, null);
                ViewData["manager_id"] = new SelectList(assignableEmployees, "employee_id", "full_name", employee.manager_id);
            }
            
            ViewData["pay_grade_id"] = new SelectList(_context.PayGrade, "pay_grade_id", "pay_grade_id", employee.pay_grade_id);
            ViewData["position_id"] = new SelectList(_context.Position, "position_id", "position_id", employee.position_id);
            ViewData["salary_type_id"] = new SelectList(_context.SalaryType, "salary_type_id", "salary_type_id", employee.salary_type_id);
            ViewData["tax_form_id"] = new SelectList(_context.TaxForm, "tax_form_id", "tax_form_id", employee.tax_form_id);
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            
            var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
            bool isHRAdmin = false;
            
            if (currentEmployeeId.HasValue)
            {
                isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
            }
            
            ViewData["contract_id"] = new SelectList(_context.Contract, "contract_id", "contract_id", employee.contract_id);
            ViewData["department_id"] = new SelectList(_context.Department, "department_id", "department_id", employee.department_id);
            
            // Filter manager list based on role
            if (isHRAdmin)
            {
                // HR Admins can assign any employee as manager (excluding self)
                ViewData["manager_id"] = new SelectList(await _context.Employee.Where(e => e.is_active == true && e.employee_id != id).ToListAsync(), "employee_id", "full_name", employee.manager_id);
            }
            else
            {
                // Non-HR users should only see employees who can be managed (excluding Managers and HR Admins)
                var assignableEmployees = await AuthorizationHelper.GetAssignableEmployeesAsync(_context, id);
                ViewData["manager_id"] = new SelectList(assignableEmployees, "employee_id", "full_name", employee.manager_id);
            }
            
            ViewData["pay_grade_id"] = new SelectList(_context.PayGrade, "pay_grade_id", "pay_grade_id", employee.pay_grade_id);
            ViewData["position_id"] = new SelectList(_context.Position, "position_id", "position_id", employee.position_id);
            ViewData["salary_type_id"] = new SelectList(_context.SalaryType, "salary_type_id", "salary_type_id", employee.salary_type_id);
            ViewData["tax_form_id"] = new SelectList(_context.TaxForm, "tax_form_id", "tax_form_id", employee.tax_form_id);
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("employee_id,first_name,last_name,full_name,national_id,date_of_birth,country_of_birth,phone,email,address,emergency_contact_name,emergency_contact_phone,relationship,biography,profile_image,employment_progress,account_status,employment_status,hire_date,is_active,profile_completion,department_id,position_id,manager_id,contract_id,tax_form_id,salary_type_id,pay_grade_id,password_hash")] Employee employee)
        {
            if (id != employee.employee_id)
            {
                return NotFound();
            }

            var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
            bool isHRAdmin = false;
            
            if (currentEmployeeId.HasValue)
            {
                isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
            }
            
            // Validate manager assignment if manager_id is provided
            if (!isHRAdmin && employee.manager_id.HasValue)
            {
                var canBeManaged = await AuthorizationHelper.CanBeManagedByLineManagerAsync(_context, employee.manager_id.Value);
                if (!canBeManaged)
                {
                    ModelState.AddModelError("manager_id", "You cannot assign employees with Manager, HR Admin, or System Admin roles. Only HR Admins can manage such assignments.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.employee_id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["contract_id"] = new SelectList(_context.Contract, "contract_id", "contract_id", employee.contract_id);
            ViewData["department_id"] = new SelectList(_context.Department, "department_id", "department_id", employee.department_id);
            
            // Filter manager list based on role
            if (isHRAdmin)
            {
                ViewData["manager_id"] = new SelectList(await _context.Employee.Where(e => e.is_active == true && e.employee_id != id).ToListAsync(), "employee_id", "full_name", employee.manager_id);
            }
            else
            {
                var assignableEmployees = await AuthorizationHelper.GetAssignableEmployeesAsync(_context, id);
                ViewData["manager_id"] = new SelectList(assignableEmployees, "employee_id", "full_name", employee.manager_id);
            }
            
            ViewData["pay_grade_id"] = new SelectList(_context.PayGrade, "pay_grade_id", "pay_grade_id", employee.pay_grade_id);
            ViewData["position_id"] = new SelectList(_context.Position, "position_id", "position_id", employee.position_id);
            ViewData["salary_type_id"] = new SelectList(_context.SalaryType, "salary_type_id", "salary_type_id", employee.salary_type_id);
            ViewData["tax_form_id"] = new SelectList(_context.TaxForm, "tax_form_id", "tax_form_id", employee.tax_form_id);
            return View(employee);
        }

        // GET: Employees/Delete/5
        // Only System Admins can delete employee profiles
        [RequireRole(AuthorizationHelper.SystemAdminRole)]
        public async Task<IActionResult> Delete(int? id, string? returnUrl = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
            if (!currentEmployeeId.HasValue)
            {
                return Unauthorized();
            }

            // Prevent self-deletion
            if (id == currentEmployeeId.Value)
            {
                TempData["ErrorMessage"] = "You cannot delete your own profile.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Index));
            }

            var employee = await _context.Employee
                .Include(e => e.contract)
                .Include(e => e.department)
                .Include(e => e.manager)
                .Include(e => e.pay_grade)
                .Include(e => e.position)
                .Include(e => e.salary_type)
                .Include(e => e.tax_form)
                .Include(e => e.Employee_Role)
                    .ThenInclude(er => er.role)
                .FirstOrDefaultAsync(m => m.employee_id == id);
            
            if (employee == null)
            {
                return NotFound();
            }

            // Get employee roles for display
            ViewBag.EmployeeRoles = employee.Employee_Role?.Select(er => er.role?.role_name).Where(r => r != null).ToList() ?? new List<string?>();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action(nameof(Index));

            _logger.LogInformation("System Admin {AdminId} accessing delete confirmation for employee {EmployeeId}", 
                currentEmployeeId.Value, id);

            return View(employee);
        }

        // POST: Employees/Delete/5
        // Only System Admins can delete employee profiles
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequireRole(AuthorizationHelper.SystemAdminRole)]
        public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl = null)
        {
            var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
            if (!currentEmployeeId.HasValue)
            {
                return Unauthorized();
            }

            // Prevent self-deletion
            if (id == currentEmployeeId.Value)
            {
                TempData["ErrorMessage"] = "You cannot delete your own profile.";
                _logger.LogWarning("System Admin {AdminId} attempted to delete their own profile", currentEmployeeId.Value);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                var employee = await _context.Employee
                    .Include(e => e.Employee_Role)
                    .FirstOrDefaultAsync(e => e.employee_id == id);
                
                if (employee == null)
                {
                    return NotFound();
                }

                var employeeName = employee.full_name;

                // Delete ALL related records first to avoid foreign key constraint issues
                
                // 1. Delete employee roles
                var employeeRoleRecords = await _context.Employee_Role.Where(er => er.employee_id == id).ToListAsync();
                if (employeeRoleRecords.Any())
                {
                    _context.Employee_Role.RemoveRange(employeeRoleRecords);
                }

                // 2. Delete notifications
                var notificationRecords = await _context.Employee_Notification.Where(en => en.employee_id == id).ToListAsync();
                if (notificationRecords.Any())
                {
                    _context.Employee_Notification.RemoveRange(notificationRecords);
                }

                // 3. Delete employee skills
                var employeeSkills = await _context.Employee_Skill.Where(es => es.employee_id == id).ToListAsync();
                if (employeeSkills.Any())
                {
                    _context.Employee_Skill.RemoveRange(employeeSkills);
                }

                // 4. Delete attendance logs and sources (must delete before attendance)
                var attendances = await _context.Attendance.Where(a => a.employee_id == id).ToListAsync();
                foreach (var attendance in attendances)
                {
                    var logs = await _context.AttendanceLog.Where(al => al.attendance_id == attendance.attendance_id).ToListAsync();
                    if (logs.Any())
                    {
                        _context.AttendanceLog.RemoveRange(logs);
                    }
                    
                    var sources = await _context.AttendanceSource.Where(als => als.attendance_id == attendance.attendance_id).ToListAsync();
                    if (sources.Any())
                    {
                        _context.AttendanceSource.RemoveRange(sources);
                    }
                }
                
                // 5. Delete attendance records
                if (attendances.Any())
                {
                    _context.Attendance.RemoveRange(attendances);
                }

                // 6. Delete attendance correction requests (both as employee and recorded_by)
                var correctionRequestsEmployee = await _context.AttendanceCorrectionRequest.Where(acr => acr.employee_id == id).ToListAsync();
                if (correctionRequestsEmployee.Any())
                {
                    _context.AttendanceCorrectionRequest.RemoveRange(correctionRequestsEmployee);
                }
                
                var correctionRequestsRecorded = await _context.AttendanceCorrectionRequest.Where(acr => acr.recorded_by == id).ToListAsync();
                if (correctionRequestsRecorded.Any())
                {
                    _context.AttendanceCorrectionRequest.RemoveRange(correctionRequestsRecorded);
                }

                // 7. Delete leave requests
                var leaveRequests = await _context.LeaveRequest.Where(lr => lr.employee_id == id).ToListAsync();
                if (leaveRequests.Any())
                {
                    _context.LeaveRequest.RemoveRange(leaveRequests);
                }

                // 8. Delete leave entitlements
                var leaveEntitlements = await _context.LeaveEntitlement.Where(le => le.employee_id == id).ToListAsync();
                if (leaveEntitlements.Any())
                {
                    _context.LeaveEntitlement.RemoveRange(leaveEntitlements);
                }

                // 9. Update missions - set manager_id to null if this employee is a manager
                var missionsAsManager = await _context.Mission.Where(m => m.manager_id == id).ToListAsync();
                foreach (var mission in missionsAsManager)
                {
                    mission.manager_id = null;
                }
                
                // Delete missions where this employee is the employee
                var missionsAsEmployee = await _context.Mission.Where(m => m.employee_id == id).ToListAsync();
                if (missionsAsEmployee.Any())
                {
                    _context.Mission.RemoveRange(missionsAsEmployee);
                }

                // 10. Delete allowance deductions
                var allowances = await _context.AllowanceDeduction.Where(ad => ad.employee_id == id).ToListAsync();
                if (allowances.Any())
                {
                    _context.AllowanceDeduction.RemoveRange(allowances);
                }

                // 11. Delete payroll logs (where employee was the actor)
                var payrollLogs = await _context.Payroll_Log.Where(pl => pl.actor == id).ToListAsync();
                if (payrollLogs.Any())
                {
                    _context.Payroll_Log.RemoveRange(payrollLogs);
                }
                
                // Also delete payroll logs for this employee's payroll records
                var employeePayrollIds = await _context.Payroll.Where(p => p.employee_id == id).Select(p => p.payroll_id).ToListAsync();
                if (employeePayrollIds.Any())
                {
                    var relatedPayrollLogs = await _context.Payroll_Log.Where(pl => employeePayrollIds.Contains(pl.payroll_id)).ToListAsync();
                    if (relatedPayrollLogs.Any())
                    {
                        _context.Payroll_Log.RemoveRange(relatedPayrollLogs);
                    }
                }

                // 12. Delete payroll records
                var payrolls = await _context.Payroll.Where(p => p.employee_id == id).ToListAsync();
                if (payrolls.Any())
                {
                    _context.Payroll.RemoveRange(payrolls);
                }

                // 13. Delete employee hierarchy (both as employee and as manager)
                var hierarchyAsEmployee = await _context.EmployeeHierarchy.Where(eh => eh.employee_id == id).ToListAsync();
                if (hierarchyAsEmployee.Any())
                {
                    _context.EmployeeHierarchy.RemoveRange(hierarchyAsEmployee);
                }
                
                var hierarchyAsManager = await _context.EmployeeHierarchy.Where(eh => eh.manager_id == id).ToListAsync();
                if (hierarchyAsManager.Any())
                {
                    _context.EmployeeHierarchy.RemoveRange(hierarchyAsManager);
                }

                // 14. Delete manager notes (both as employee and as manager)
                var notesAsEmployee = await _context.ManagerNotes.Where(mn => mn.employee_id == id).ToListAsync();
                if (notesAsEmployee.Any())
                {
                    _context.ManagerNotes.RemoveRange(notesAsEmployee);
                }
                
                var notesAsManager = await _context.ManagerNotes.Where(mn => mn.manager_id == id).ToListAsync();
                if (notesAsManager.Any())
                {
                    _context.ManagerNotes.RemoveRange(notesAsManager);
                }

                // 15. Delete devices
                var devices = await _context.Device.Where(d => d.employee_id == id).ToListAsync();
                if (devices.Any())
                {
                    _context.Device.RemoveRange(devices);
                }

                // 16. Delete shift assignments
                var shiftAssignments = await _context.ShiftAssignment.Where(sa => sa.employee_id == id).ToListAsync();
                if (shiftAssignments.Any())
                {
                    _context.ShiftAssignment.RemoveRange(shiftAssignments);
                }

                // 17. Delete reimbursements
                var reimbursements = await _context.Reimbursement.Where(r => r.employee_id == id).ToListAsync();
                if (reimbursements.Any())
                {
                    _context.Reimbursement.RemoveRange(reimbursements);
                }

                // 18. Update employees managed by this employee (set manager_id to null)
                var managedEmployees = await _context.Employee.Where(e => e.manager_id == id).ToListAsync();
                foreach (var managedEmployee in managedEmployees)
                {
                    managedEmployee.manager_id = null;
                }

                // 19. Update departments managed by this employee (set department_head_id to null)
                var managedDepartments = await _context.Department.Where(d => d.department_head_id == id).ToListAsync();
                foreach (var department in managedDepartments)
                {
                    department.department_head_id = null;
                }

                // 20. Delete specialist records
                var hrAdmin = await _context.HRAdministrator.FirstOrDefaultAsync(hr => hr.employee_id == id);
                if (hrAdmin != null)
                {
                    _context.HRAdministrator.Remove(hrAdmin);
                }
                
                var lineManager = await _context.LineManager.FirstOrDefaultAsync(lm => lm.employee_id == id);
                if (lineManager != null)
                {
                    _context.LineManager.Remove(lineManager);
                }
                
                var payrollSpecialist = await _context.PayrollSpecialist.FirstOrDefaultAsync(ps => ps.employee_id == id);
                if (payrollSpecialist != null)
                {
                    _context.PayrollSpecialist.Remove(payrollSpecialist);
                }

                // 21. Save all changes before deleting employee
                await _context.SaveChangesAsync();

                // 22. Finally, delete the employee
                _context.Employee.Remove(employee);
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                _logger.LogInformation("System Admin {AdminId} deleted employee {EmployeeId} ({EmployeeName})", 
                    currentEmployeeId.Value, id, employeeName);

                TempData["SuccessMessage"] = $"Employee '{employeeName}' has been successfully deleted.";
                
                // Redirect back to the page they came from, or default to Employees/Index
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
                TempData["ErrorMessage"] = "Cannot delete this employee because they have related records (attendance, leave requests, etc.). Consider deactivating the employee instead.";
                return RedirectToAction(nameof(Delete), new { id, returnUrl });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting employee {EmployeeId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the employee. Please try again.";
                return RedirectToAction(nameof(Delete), new { id, returnUrl });
            }
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employee.Any(e => e.employee_id == id);
        }
    }
}

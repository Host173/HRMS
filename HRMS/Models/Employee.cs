using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Employee
{
    public int employee_id { get; set; }

    public string? first_name { get; set; }

    public string? last_name { get; set; }

    public string? full_name { get; set; }

    public string? national_id { get; set; }

    public DateOnly? date_of_birth { get; set; }

    public string? country_of_birth { get; set; }

    public string? phone { get; set; }

    public string? email { get; set; }

    public string? address { get; set; }

    public string? emergency_contact_name { get; set; }

    public string? emergency_contact_phone { get; set; }

    public string? relationship { get; set; }

    public string? biography { get; set; }

    public string? profile_image { get; set; }

    public string? employment_progress { get; set; }

    public string? account_status { get; set; }

    public string? employment_status { get; set; }

    public DateOnly? hire_date { get; set; }

    public bool? is_active { get; set; }

    public int? profile_completion { get; set; }

    public int? department_id { get; set; }

    public int? position_id { get; set; }

    public int? manager_id { get; set; }

    public int? contract_id { get; set; }

    public int? tax_form_id { get; set; }

    public int? salary_type_id { get; set; }

    public int? pay_grade_id { get; set; }

    public string? password_hash { get; set; }

    public virtual ICollection<AllowanceDeduction> AllowanceDeduction { get; set; } = new List<AllowanceDeduction>();

    public virtual ICollection<Attendance> Attendance { get; set; } = new List<Attendance>();

    public virtual ICollection<AttendanceCorrectionRequest> AttendanceCorrectionRequestemployee { get; set; } = new List<AttendanceCorrectionRequest>();

    public virtual ICollection<AttendanceCorrectionRequest> AttendanceCorrectionRequestrecorded_byNavigation { get; set; } = new List<AttendanceCorrectionRequest>();

    public virtual ICollection<Department> Department { get; set; } = new List<Department>();

    public virtual ICollection<Device> Device { get; set; } = new List<Device>();

    public virtual ICollection<EmployeeHierarchy> EmployeeHierarchyemployee { get; set; } = new List<EmployeeHierarchy>();

    public virtual ICollection<EmployeeHierarchy> EmployeeHierarchymanager { get; set; } = new List<EmployeeHierarchy>();

    public virtual ICollection<Employee_Notification> Employee_Notification { get; set; } = new List<Employee_Notification>();

    public virtual ICollection<Employee_Role> Employee_Role { get; set; } = new List<Employee_Role>();

    public virtual ICollection<Employee_Skill> Employee_Skill { get; set; } = new List<Employee_Skill>();

    public virtual HRAdministrator? HRAdministrator { get; set; }

    public virtual ICollection<Employee> Inversemanager { get; set; } = new List<Employee>();

    public virtual ICollection<LeaveEntitlement> LeaveEntitlement { get; set; } = new List<LeaveEntitlement>();

    public virtual ICollection<LeaveRequest> LeaveRequest { get; set; } = new List<LeaveRequest>();

    public virtual LineManager? LineManager { get; set; }

    public virtual ICollection<ManagerNotes> ManagerNotesemployee { get; set; } = new List<ManagerNotes>();

    public virtual ICollection<ManagerNotes> ManagerNotesmanager { get; set; } = new List<ManagerNotes>();

    public virtual ICollection<Mission> Missionemployee { get; set; } = new List<Mission>();

    public virtual ICollection<Mission> Missionmanager { get; set; } = new List<Mission>();

    public virtual ICollection<Payroll> Payroll { get; set; } = new List<Payroll>();

    public virtual PayrollSpecialist? PayrollSpecialist { get; set; }

    public virtual ICollection<Payroll_Log> Payroll_Log { get; set; } = new List<Payroll_Log>();

    public virtual ICollection<Reimbursement> Reimbursement { get; set; } = new List<Reimbursement>();

    public virtual ICollection<ShiftAssignment> ShiftAssignment { get; set; } = new List<ShiftAssignment>();

    public virtual SystemAdministrator? SystemAdministrator { get; set; }

    public virtual Contract? contract { get; set; }

    public virtual Department? department { get; set; }

    public virtual Employee? manager { get; set; }

    public virtual PayGrade? pay_grade { get; set; }

    public virtual Position? position { get; set; }

    public virtual SalaryType? salary_type { get; set; }

    public virtual TaxForm? tax_form { get; set; }

    public virtual ICollection<Exception> exception { get; set; } = new List<Exception>();

    public virtual ICollection<Verification> verification { get; set; } = new List<Verification>();
}

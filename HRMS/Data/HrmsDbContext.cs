using System;
using System.Collections.Generic;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Data;

public partial class HrmsDbContext : DbContext
{
    public HrmsDbContext()
    {
    }

    public HrmsDbContext(DbContextOptions<HrmsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AllowanceDeduction> AllowanceDeduction { get; set; }

    public virtual DbSet<ApprovalWorkflow> ApprovalWorkflow { get; set; }

    public virtual DbSet<ApprovalWorkflowStep> ApprovalWorkflowStep { get; set; }

    public virtual DbSet<Attendance> Attendance { get; set; }

    public virtual DbSet<AttendanceCorrectionRequest> AttendanceCorrectionRequest { get; set; }

    public virtual DbSet<AttendanceLog> AttendanceLog { get; set; }

    public virtual DbSet<AttendanceSource> AttendanceSource { get; set; }

    public virtual DbSet<BonusPolicy> BonusPolicy { get; set; }

    public virtual DbSet<ConsultantContract> ConsultantContract { get; set; }

    public virtual DbSet<Contract> Contract { get; set; }

    public virtual DbSet<Currency> Currency { get; set; }

    public virtual DbSet<DeductionPolicy> DeductionPolicy { get; set; }

    public virtual DbSet<Department> Department { get; set; }

    public virtual DbSet<Device> Device { get; set; }

    public virtual DbSet<Employee> Employee { get; set; }

    public virtual DbSet<EmployeeHierarchy> EmployeeHierarchy { get; set; }

    public virtual DbSet<Employee_Notification> Employee_Notification { get; set; }

    public virtual DbSet<Employee_Role> Employee_Role { get; set; }

    public virtual DbSet<Employee_Skill> Employee_Skill { get; set; }

    public virtual DbSet<HRMS.Models.Exception> Exception { get; set; }

    public virtual DbSet<FullTimeContract> FullTimeContract { get; set; }

    public virtual DbSet<HRAdministrator> HRAdministrator { get; set; }

    public virtual DbSet<HolidayLeave> HolidayLeave { get; set; }

    public virtual DbSet<Insurance> Insurance { get; set; }

    public virtual DbSet<InternshipContract> InternshipContract { get; set; }

    public virtual DbSet<LatenessPolicy> LatenessPolicy { get; set; }

    public virtual DbSet<Leave> Leave { get; set; }

    public virtual DbSet<LeaveDocument> LeaveDocument { get; set; }

    public virtual DbSet<LeaveEntitlement> LeaveEntitlement { get; set; }

    public virtual DbSet<LeavePolicy> LeavePolicy { get; set; }

    public virtual DbSet<LeaveRequest> LeaveRequest { get; set; }

    public virtual DbSet<LineManager> LineManager { get; set; }

    public virtual DbSet<ManagerNotes> ManagerNotes { get; set; }

    public virtual DbSet<Mission> Mission { get; set; }

    public virtual DbSet<Notification> Notification { get; set; }

    public virtual DbSet<OvertimePolicy> OvertimePolicy { get; set; }

    public virtual DbSet<PartTimeContract> PartTimeContract { get; set; }

    public virtual DbSet<PayGrade> PayGrade { get; set; }

    public virtual DbSet<Payroll> Payroll { get; set; }

    public virtual DbSet<PayrollPeriod> PayrollPeriod { get; set; }

    public virtual DbSet<PayrollPolicy> PayrollPolicy { get; set; }

    public virtual DbSet<PayrollPolicy_ID> PayrollPolicy_ID { get; set; }

    public virtual DbSet<PayrollSpecialist> PayrollSpecialist { get; set; }

    public virtual DbSet<Payroll_Log> Payroll_Log { get; set; }

    public virtual DbSet<Position> Position { get; set; }

    public virtual DbSet<ProbationLeave> ProbationLeave { get; set; }

    public virtual DbSet<Reimbursement> Reimbursement { get; set; }

    public virtual DbSet<Role> Role { get; set; }

    public virtual DbSet<RolePermission> RolePermission { get; set; }

    public virtual DbSet<SalaryType> SalaryType { get; set; }

    public virtual DbSet<ShiftAssignment> ShiftAssignment { get; set; }

    public virtual DbSet<ShiftCycle> ShiftCycle { get; set; }

    public virtual DbSet<ShiftCycleAssignment> ShiftCycleAssignment { get; set; }

    public virtual DbSet<ShiftSchedule> ShiftSchedule { get; set; }

    public virtual DbSet<SickLeave> SickLeave { get; set; }

    public virtual DbSet<Skill> Skill { get; set; }

    public virtual DbSet<SystemAdministrator> SystemAdministrator { get; set; }

    public virtual DbSet<TaxForm> TaxForm { get; set; }

    public virtual DbSet<Termination> Termination { get; set; }

    public virtual DbSet<VacationLeave> VacationLeave { get; set; }

    public virtual DbSet<Verification> Verification { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){}
    


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllowanceDeduction>(entity =>
        {
            entity.HasKey(e => e.ad_id).HasName("PK__Allowanc__CAA4A6271469BE18");

            entity.Property(e => e.amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.currency_code).HasMaxLength(3);
            entity.Property(e => e.duration).HasMaxLength(100);
            entity.Property(e => e.timezone).HasMaxLength(50);
            entity.Property(e => e.type).HasMaxLength(100);

            entity.HasOne(d => d.currency_codeNavigation).WithMany(p => p.AllowanceDeduction)
                .HasForeignKey(d => d.currency_code)
                .HasConstraintName("FK_AllowanceDeduction_Currency");

            entity.HasOne(d => d.employee).WithMany(p => p.AllowanceDeduction)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AllowanceDeduction_Employee");

            entity.HasOne(d => d.payroll).WithMany(p => p.AllowanceDeduction)
                .HasForeignKey(d => d.payroll_id)
                .HasConstraintName("FK_AllowanceDeduction_Payroll");
        });

        modelBuilder.Entity<ApprovalWorkflow>(entity =>
        {
            entity.HasKey(e => e.workflow_id).HasName("PK__Approval__64A76B704F06DF85");

            entity.Property(e => e.approver_role)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.status).HasMaxLength(50);
            entity.Property(e => e.threshold_amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.workflow_type).HasMaxLength(100);
        });

        modelBuilder.Entity<ApprovalWorkflowStep>(entity =>
        {
            entity.HasKey(e => e.approval_workflow_step_id).HasName("PK__Approval__851F8ED534434319");

            entity.Property(e => e.action_required).HasMaxLength(200);

            entity.HasOne(d => d.role).WithMany(p => p.ApprovalWorkflowStep)
                .HasForeignKey(d => d.role_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalWorkflowStep_Role");

            entity.HasOne(d => d.workflow).WithMany(p => p.ApprovalWorkflowStep)
                .HasForeignKey(d => d.workflow_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalWorkflowStep_Workflow");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.attendance_id).HasName("PK__Attendan__20D6A968BC2E773C");

            entity.Property(e => e.duration).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.login_method).HasMaxLength(100);
            entity.Property(e => e.logout_method).HasMaxLength(100);

            entity.HasOne(d => d.employee).WithMany(p => p.Attendance)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Employee");

            entity.HasOne(d => d.exception).WithMany(p => p.Attendance)
                .HasForeignKey(d => d.exception_id)
                .HasConstraintName("FK_Attendance_Exception");

            entity.HasOne(d => d.shift).WithMany(p => p.Attendance)
                .HasForeignKey(d => d.shift_id)
                .HasConstraintName("FK_Attendance_Shift");
        });

        modelBuilder.Entity<AttendanceCorrectionRequest>(entity =>
        {
            entity.HasKey(e => e.attendance_correction_request_id).HasName("PK__Attendan__44FC2ECEAB46825D");

            entity.Property(e => e.correction_type).HasMaxLength(100);
            entity.Property(e => e.status).HasMaxLength(50);

            entity.HasOne(d => d.employee).WithMany(p => p.AttendanceCorrectionRequestemployee)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceCorrectionRequest_Employee");

            entity.HasOne(d => d.recorded_byNavigation).WithMany(p => p.AttendanceCorrectionRequestrecorded_byNavigation)
                .HasForeignKey(d => d.recorded_by)
                .HasConstraintName("FK_AttendanceCorrectionRequest_RecordedBy");
        });

        modelBuilder.Entity<AttendanceLog>(entity =>
        {
            entity.HasKey(e => e.attendance_log_id).HasName("PK__Attendan__DB38FB09D0C95738");

            entity.Property(e => e.timestamp).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.attendance).WithMany(p => p.AttendanceLog)
                .HasForeignKey(d => d.attendance_id)
                .HasConstraintName("FK_AttendanceLog_Attendance");
        });

        modelBuilder.Entity<AttendanceSource>(entity =>
        {
            entity.HasKey(e => new { e.attendance_id, e.device_id });

            entity.Property(e => e.latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.recorded_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.source_type).HasMaxLength(100);

            entity.HasOne(d => d.attendance).WithMany(p => p.AttendanceSource)
                .HasForeignKey(d => d.attendance_id)
                .HasConstraintName("FK_AttendanceSource_Attendance");

            entity.HasOne(d => d.device).WithMany(p => p.AttendanceSource)
                .HasForeignKey(d => d.device_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceSource_Device");
        });

        modelBuilder.Entity<BonusPolicy>(entity =>
        {
            entity.HasKey(e => e.policy_id).HasName("PK__BonusPol__47DA3F039740B91D");

            entity.Property(e => e.policy_id).ValueGeneratedNever();
            entity.Property(e => e.bonus_type).HasMaxLength(100);

            entity.HasOne(d => d.policy).WithOne(p => p.BonusPolicy)
                .HasForeignKey<BonusPolicy>(d => d.policy_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BonusPolicy_Policy");
        });

        modelBuilder.Entity<ConsultantContract>(entity =>
        {
            entity.HasKey(e => e.contract_id).HasName("PK__Consulta__F8D6642308759C24");

            entity.Property(e => e.contract_id).ValueGeneratedNever();
            entity.Property(e => e.fees).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.payment_schedule).HasMaxLength(255);

            entity.HasOne(d => d.contract).WithOne(p => p.ConsultantContract)
                .HasForeignKey<ConsultantContract>(d => d.contract_id)
                .HasConstraintName("FK_ConsultantContract_Contract");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.contract_id).HasName("PK__Contract__F8D6642304497363");

            entity.Property(e => e.current_state).HasMaxLength(50);
            entity.Property(e => e.type).HasMaxLength(50);
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(e => e.CurrencyCode).HasName("PK__Currency__408426BEC67CE49D");

            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrencyName).HasMaxLength(50);
            entity.Property(e => e.ExchangeRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<DeductionPolicy>(entity =>
        {
            entity.HasKey(e => e.policy_id).HasName("PK__Deductio__47DA3F036A318BFE");

            entity.Property(e => e.policy_id).ValueGeneratedNever();
            entity.Property(e => e.calculation_mode).HasMaxLength(100);
            entity.Property(e => e.deduction_reason).HasMaxLength(200);

            entity.HasOne(d => d.policy).WithOne(p => p.DeductionPolicy)
                .HasForeignKey<DeductionPolicy>(d => d.policy_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeductionPolicy_Policy");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.department_id).HasName("PK__Departme__C2232422AF7D3E8E");

            entity.Property(e => e.department_name).HasMaxLength(150);

            entity.HasOne(d => d.department_head).WithMany(p => p.Department)
                .HasForeignKey(d => d.department_head_id)
                .HasConstraintName("FK_Department_DepartmentHead");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.device_id).HasName("PK__Device__3B085D8B1D1D5EC5");

            entity.Property(e => e.device_type).HasMaxLength(100);
            entity.Property(e => e.latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.terminal_id).HasMaxLength(100);

            entity.HasOne(d => d.employee).WithMany(p => p.Device)
                .HasForeignKey(d => d.employee_id)
                .HasConstraintName("FK_Device_Employee");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.employee_id).HasName("PK__Employee__C52E0BA88FCEC276");

            entity.Property(e => e.account_status).HasMaxLength(50);
            entity.Property(e => e.country_of_birth).HasMaxLength(100);
            entity.Property(e => e.email).HasMaxLength(200);
            entity.Property(e => e.emergency_contact_name).HasMaxLength(200);
            entity.Property(e => e.emergency_contact_phone).HasMaxLength(50);
            entity.Property(e => e.employment_progress).HasMaxLength(100);
            entity.Property(e => e.employment_status).HasMaxLength(50);
            entity.Property(e => e.first_name).HasMaxLength(100);
            entity.Property(e => e.full_name).HasMaxLength(201);
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.last_name).HasMaxLength(100);
            entity.Property(e => e.national_id).HasMaxLength(50);
            entity.Property(e => e.password_hash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.phone).HasMaxLength(50);
            entity.Property(e => e.profile_completion).HasDefaultValue(0);
            entity.Property(e => e.profile_image).HasMaxLength(500);
            entity.Property(e => e.relationship).HasMaxLength(100);

            entity.HasOne(d => d.contract).WithMany(p => p.Employee)
                .HasForeignKey(d => d.contract_id)
                .HasConstraintName("FK_Employee_Contract");

            entity.HasOne(d => d.department).WithMany(p => p.Employee)
                .HasForeignKey(d => d.department_id)
                .HasConstraintName("FK_Employee_Department");

            entity.HasOne(d => d.manager).WithMany(p => p.Inversemanager)
                .HasForeignKey(d => d.manager_id)
                .HasConstraintName("FK_Employee_Manager");

            entity.HasOne(d => d.pay_grade).WithMany(p => p.Employee)
                .HasForeignKey(d => d.pay_grade_id)
                .HasConstraintName("FK_Employee_PayGrade");

            entity.HasOne(d => d.position).WithMany(p => p.Employee)
                .HasForeignKey(d => d.position_id)
                .HasConstraintName("FK_Employee_Position");

            entity.HasOne(d => d.salary_type).WithMany(p => p.Employee)
                .HasForeignKey(d => d.salary_type_id)
                .HasConstraintName("FK_Employee_SalaryType");

            entity.HasOne(d => d.tax_form).WithMany(p => p.Employee)
                .HasForeignKey(d => d.tax_form_id)
                .HasConstraintName("FK_Employee_TaxForm");

            entity.HasMany(d => d.exception).WithMany(p => p.employee)
                .UsingEntity<Dictionary<string, object>>(
                    "Employee_Exception",
                    r => r.HasOne<HRMS.Models.Exception>().WithMany()
                        .HasForeignKey("exception_id")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EmployeeException_Exception"),
                    l => l.HasOne<Employee>().WithMany()
                        .HasForeignKey("employee_id")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EmployeeException_Employee"),
                    j =>
                    {
                        j.HasKey("employee_id", "exception_id");
                    });

            entity.HasMany(d => d.verification).WithMany(p => p.employee)
                .UsingEntity<Dictionary<string, object>>(
                    "Employee_Verification",
                    r => r.HasOne<Verification>().WithMany()
                        .HasForeignKey("verification_id")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EmployeeVerification_Verification"),
                    l => l.HasOne<Employee>().WithMany()
                        .HasForeignKey("employee_id")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EmployeeVerification_Employee"),
                    j =>
                    {
                        j.HasKey("employee_id", "verification_id");
                    });
        });

        modelBuilder.Entity<EmployeeHierarchy>(entity =>
        {
            entity.HasKey(e => e.employee_hierarchy_id).HasName("PK__Employee__7A60D94821222FF5");

            entity.HasOne(d => d.employee).WithMany(p => p.EmployeeHierarchyemployee)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeHierarchy_Employee");

            entity.HasOne(d => d.manager).WithMany(p => p.EmployeeHierarchymanager)
                .HasForeignKey(d => d.manager_id)
                .HasConstraintName("FK_EmployeeHierarchy_Manager");
        });

        modelBuilder.Entity<Employee_Notification>(entity =>
        {
            entity.HasKey(e => e.employee_notification_id).HasName("PK__Employee__1632191CB317DCB5");

            entity.Property(e => e.delivery_status).HasMaxLength(50);

            entity.HasOne(d => d.employee).WithMany(p => p.Employee_Notification)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeNotification_Employee");

            entity.HasOne(d => d.notification).WithMany(p => p.Employee_Notification)
                .HasForeignKey(d => d.notification_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeNotification_Notification");
        });

        modelBuilder.Entity<Employee_Role>(entity =>
        {
            entity.HasKey(e => new { e.employee_id, e.role_id });

            entity.HasOne(d => d.employee).WithMany(p => p.Employee_Role)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeRole_Employee");

            entity.HasOne(d => d.role).WithMany(p => p.Employee_Role)
                .HasForeignKey(d => d.role_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeRole_Role");
        });

        modelBuilder.Entity<Employee_Skill>(entity =>
        {
            entity.HasKey(e => new { e.employee_id, e.skill_id });

            entity.Property(e => e.proficiency_level).HasMaxLength(50);

            entity.HasOne(d => d.employee).WithMany(p => p.Employee_Skill)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeSkill_Employee");

            entity.HasOne(d => d.skill).WithMany(p => p.Employee_Skill)
                .HasForeignKey(d => d.skill_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeSkill_Skill");
        });

        modelBuilder.Entity<HRMS.Models.Exception>(entity =>
        {
            entity.HasKey(e => e.exception_id).HasName("PK__Exceptio__C42DECC2E158AD80");

            entity.Property(e => e.category).HasMaxLength(100);
            entity.Property(e => e.name).HasMaxLength(200);
            entity.Property(e => e.status).HasMaxLength(50);
        });

        modelBuilder.Entity<FullTimeContract>(entity =>
        {
            entity.HasKey(e => e.contract_id).HasName("PK__FullTime__F8D66423152F1A3A");

            entity.Property(e => e.contract_id).ValueGeneratedNever();

            entity.HasOne(d => d.contract).WithOne(p => p.FullTimeContract)
                .HasForeignKey<FullTimeContract>(d => d.contract_id)
                .HasConstraintName("FK_FullTimeContract_Contract");
        });

        modelBuilder.Entity<HRAdministrator>(entity =>
        {
            entity.HasKey(e => e.employee_id).HasName("PK__HRAdmini__C52E0BA8369B96F7");

            entity.Property(e => e.employee_id).ValueGeneratedNever();
            entity.Property(e => e.password_hash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.record_access_scope).HasMaxLength(200);

            entity.HasOne(d => d.employee).WithOne(p => p.HRAdministrator)
                .HasForeignKey<HRAdministrator>(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HRAdministrator_Employee");
        });

        modelBuilder.Entity<HolidayLeave>(entity =>
        {
            entity.HasKey(e => e.leave_id).HasName("PK__HolidayL__743350BC11F18817");

            entity.Property(e => e.leave_id).ValueGeneratedNever();
            entity.Property(e => e.holiday_name).HasMaxLength(200);
            entity.Property(e => e.regional_scope).HasMaxLength(200);

            entity.HasOne(d => d.leave).WithOne(p => p.HolidayLeave)
                .HasForeignKey<HolidayLeave>(d => d.leave_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HolidayLeave_Leave");
        });

        modelBuilder.Entity<Insurance>(entity =>
        {
            entity.HasKey(e => e.insurance_id).HasName("PK__Insuranc__58B60F4512831E7D");

            entity.Property(e => e.contribution_rate).HasColumnType("decimal(6, 4)");
            entity.Property(e => e.type).HasMaxLength(100);
        });

        modelBuilder.Entity<InternshipContract>(entity =>
        {
            entity.HasKey(e => e.contract_id).HasName("PK__Internsh__F8D66423761BD03A");

            entity.Property(e => e.contract_id).ValueGeneratedNever();
            entity.Property(e => e.stipend_related).HasMaxLength(255);

            entity.HasOne(d => d.contract).WithOne(p => p.InternshipContract)
                .HasForeignKey<InternshipContract>(d => d.contract_id)
                .HasConstraintName("FK_InternshipContract_Contract");
        });

        modelBuilder.Entity<LatenessPolicy>(entity =>
        {
            entity.HasKey(e => e.policy_id).HasName("PK__Lateness__47DA3F034AE24A92");

            entity.Property(e => e.policy_id).ValueGeneratedNever();
            entity.Property(e => e.deduction_rate).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.policy).WithOne(p => p.LatenessPolicy)
                .HasForeignKey<LatenessPolicy>(d => d.policy_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LatenessPolicy_Policy");
        });

        modelBuilder.Entity<Leave>(entity =>
        {
            entity.HasKey(e => e.leave_id).HasName("PK__Leave__743350BCEA7D4376");

            entity.Property(e => e.leave_type).HasMaxLength(100);
        });

        modelBuilder.Entity<LeaveDocument>(entity =>
        {
            entity.HasKey(e => e.document_id).HasName("PK__LeaveDoc__9666E8AC1CAA3E69");

            entity.Property(e => e.file_path).HasMaxLength(500);
            entity.Property(e => e.uploaded_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.leave_request).WithMany(p => p.LeaveDocument)
                .HasForeignKey(d => d.leave_request_id)
                .HasConstraintName("FK_LeaveDocument_LeaveRequest");
        });

        modelBuilder.Entity<LeaveEntitlement>(entity =>
        {
            entity.HasKey(e => e.leave_entitlement_id).HasName("PK__LeaveEnt__11C76BEE35BA0F6C");

            entity.Property(e => e.entitlement).HasColumnType("decimal(8, 2)");

            entity.HasOne(d => d.employee).WithMany(p => p.LeaveEntitlement)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveEntitlement_Employee");

            entity.HasOne(d => d.leave_type).WithMany(p => p.LeaveEntitlement)
                .HasForeignKey(d => d.leave_type_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveEntitlement_Leave");
        });

        modelBuilder.Entity<LeavePolicy>(entity =>
        {
            entity.HasKey(e => e.policy_id).HasName("PK__LeavePol__47DA3F0322F9DD37");

            entity.Property(e => e.name).HasMaxLength(200);
            entity.Property(e => e.special_leave_type).HasMaxLength(100);

            // Map newer LeavePolicy columns (DB already contains them; do NOT change schema)
            entity.Property(e => e.leave_type_id);
            entity.Property(e => e.documentation_requirements);
            entity.Property(e => e.approval_workflow).HasMaxLength(500);
            entity.Property(e => e.is_active);
            entity.Property(e => e.requires_hr_admin_approval);
            entity.Property(e => e.max_days_per_request);
            entity.Property(e => e.min_days_per_request);
            entity.Property(e => e.requires_documentation);

            // Explicitly map navigation FK to avoid EF creating a shadow FK like "leave_typeleave_id"
            entity.HasOne(d => d.leave_type).WithMany()
                .HasForeignKey(d => d.leave_type_id)
                .HasConstraintName("FK_LeavePolicy_Leave");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.request_id).HasName("PK__LeaveReq__18D3B90F8653FC24");

            entity.Property(e => e.approval_timing).HasMaxLength(100);
            entity.Property(e => e.status).HasMaxLength(50);

            entity.HasOne(d => d.employee).WithMany(p => p.LeaveRequest)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveRequest_Employee");

            entity.HasOne(d => d.leave).WithMany(p => p.LeaveRequest)
                .HasForeignKey(d => d.leave_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveRequest_Leave");
        });

        modelBuilder.Entity<LineManager>(entity =>
        {
            entity.HasKey(e => e.employee_id).HasName("PK__LineMana__C52E0BA8C1E6B889");

            entity.Property(e => e.employee_id).ValueGeneratedNever();
            entity.Property(e => e.approval_limit).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.supervised_departments).HasMaxLength(200);

            entity.HasOne(d => d.employee).WithOne(p => p.LineManager)
                .HasForeignKey<LineManager>(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LineManager_Employee");
        });

        modelBuilder.Entity<ManagerNotes>(entity =>
        {
            entity.HasKey(e => e.note_id).HasName("PK__ManagerN__CEDD0FA41546AC10");

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.employee).WithMany(p => p.ManagerNotesemployee)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ManagerNotes_Employee");

            entity.HasOne(d => d.manager).WithMany(p => p.ManagerNotesmanager)
                .HasForeignKey(d => d.manager_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ManagerNotes_Manager");
        });

        modelBuilder.Entity<Mission>(entity =>
        {
            entity.HasKey(e => e.mission_id).HasName("PK__Mission__B5419AB2FBF65C13");

            entity.Property(e => e.destination).HasMaxLength(255);
            entity.Property(e => e.status).HasMaxLength(50);

            entity.HasOne(d => d.employee).WithMany(p => p.Missionemployee)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mission_Employee");

            entity.HasOne(d => d.manager).WithMany(p => p.Missionmanager)
                .HasForeignKey(d => d.manager_id)
                .HasConstraintName("FK_Mission_Manager");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.notification_id).HasName("PK__Notifica__E059842F66E455D0");

            entity.Property(e => e.notification_type).HasMaxLength(100);
            entity.Property(e => e.timestamp).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.urgency).HasMaxLength(50);
        });

        modelBuilder.Entity<OvertimePolicy>(entity =>
        {
            entity.HasKey(e => e.policy_id).HasName("PK__Overtime__47DA3F03C2023F7F");

            entity.Property(e => e.policy_id).ValueGeneratedNever();
            entity.Property(e => e.weekday_rate_multiplier).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.weekend_rate_multiplier).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.policy).WithOne(p => p.OvertimePolicy)
                .HasForeignKey<OvertimePolicy>(d => d.policy_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OvertimePolicy_Policy");
        });

        modelBuilder.Entity<PartTimeContract>(entity =>
        {
            entity.HasKey(e => e.contract_id).HasName("PK__PartTime__F8D6642363212E76");

            entity.Property(e => e.contract_id).ValueGeneratedNever();
            entity.Property(e => e.hourly_rate).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.contract).WithOne(p => p.PartTimeContract)
                .HasForeignKey<PartTimeContract>(d => d.contract_id)
                .HasConstraintName("FK_PartTimeContract_Contract");
        });

        modelBuilder.Entity<PayGrade>(entity =>
        {
            entity.HasKey(e => e.pay_grade_id).HasName("PK__PayGrade__C8AD0DEDC4AC3E1C");

            entity.Property(e => e.grade_name).HasMaxLength(50);
            entity.Property(e => e.max_salary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.min_salary).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasKey(e => e.payroll_id).HasName("PK__Payroll__D99FC944F90D41ED");

            entity.Property(e => e.actual_pay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.adjustments).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.base_amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.contributions).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.net_salary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.taxes).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.employee).WithMany(p => p.Payroll)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payroll_Employee");
        });

        modelBuilder.Entity<PayrollPeriod>(entity =>
        {
            entity.HasKey(e => e.payroll_period_id).HasName("PK__PayrollP__CD8483A252A9E182");

            entity.Property(e => e.status).HasMaxLength(50);

            entity.HasOne(d => d.payroll).WithMany(p => p.PayrollPeriod)
                .HasForeignKey(d => d.payroll_id)
                .HasConstraintName("FK_PayrollPeriod_Payroll");
        });

        modelBuilder.Entity<PayrollPolicy>(entity =>
        {
            entity.HasKey(e => e.policy_id).HasName("PK__PayrollP__47DA3F03A34810FE");

            entity.Property(e => e.type).HasMaxLength(100);
        });

        modelBuilder.Entity<PayrollPolicy_ID>(entity =>
        {
            entity.HasKey(e => e.payroll_policy_id).HasName("PK__PayrollP__9F861004B4A13950");

            entity.HasOne(d => d.payroll).WithMany(p => p.PayrollPolicy_ID)
                .HasForeignKey(d => d.payroll_id)
                .HasConstraintName("FK_PayrollPolicyID_Payroll");

            entity.HasOne(d => d.policy).WithMany(p => p.PayrollPolicy_ID)
                .HasForeignKey(d => d.policy_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollPolicyID_Policy");
        });

        modelBuilder.Entity<PayrollSpecialist>(entity =>
        {
            entity.HasKey(e => e.employee_id).HasName("PK__PayrollS__C52E0BA8826E0C92");

            entity.Property(e => e.employee_id).ValueGeneratedNever();
            entity.Property(e => e.assigned_region).HasMaxLength(100);
            entity.Property(e => e.processing_frequency).HasMaxLength(50);

            entity.HasOne(d => d.employee).WithOne(p => p.PayrollSpecialist)
                .HasForeignKey<PayrollSpecialist>(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollSpecialist_Employee");
        });

        modelBuilder.Entity<Payroll_Log>(entity =>
        {
            entity.HasKey(e => e.payroll_log_id).HasName("PK__Payroll___7B69DA7A42284C66");

            entity.Property(e => e.change_date).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.modification_type).HasMaxLength(100);

            entity.HasOne(d => d.actorNavigation).WithMany(p => p.Payroll_Log)
                .HasForeignKey(d => d.actor)
                .HasConstraintName("FK_PayrollLog_Actor");

            entity.HasOne(d => d.payroll).WithMany(p => p.Payroll_Log)
                .HasForeignKey(d => d.payroll_id)
                .HasConstraintName("FK_PayrollLog_Payroll");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.position_id).HasName("PK__Position__99A0E7A45F347D41");

            entity.Property(e => e.position_title).HasMaxLength(100);
            entity.Property(e => e.status).HasMaxLength(50);
        });

        modelBuilder.Entity<ProbationLeave>(entity =>
        {
            entity.HasKey(e => e.leave_id).HasName("PK__Probatio__743350BCB8996FFB");

            entity.Property(e => e.leave_id).ValueGeneratedNever();

            entity.HasOne(d => d.leave).WithOne(p => p.ProbationLeave)
                .HasForeignKey<ProbationLeave>(d => d.leave_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProbationLeave_Leave");
        });

        modelBuilder.Entity<Reimbursement>(entity =>
        {
            entity.HasKey(e => e.reimbursement_id).HasName("PK__Reimburs__F6C26984BFD885B1");

            entity.Property(e => e.claim_type).HasMaxLength(100);
            entity.Property(e => e.current_status).HasMaxLength(50);
            entity.Property(e => e.type).HasMaxLength(100);

            entity.HasOne(d => d.employee).WithMany(p => p.Reimbursement)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reimbursement_Employee");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.role_id).HasName("PK__Role__760965CCB1BFF08A");

            entity.Property(e => e.purpose).HasMaxLength(255);
            entity.Property(e => e.role_name).HasMaxLength(100);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.role_id, e.permission_name });

            entity.Property(e => e.permission_name).HasMaxLength(150);
            entity.Property(e => e.allowed_action).HasMaxLength(150);

            entity.HasOne(d => d.role).WithMany(p => p.RolePermission)
                .HasForeignKey(d => d.role_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Role");
        });

        modelBuilder.Entity<SalaryType>(entity =>
        {
            entity.HasKey(e => e.salary_type_id).HasName("PK__SalaryTy__4D6470627C0D0205");

            entity.Property(e => e.currency_code).HasMaxLength(3);
            entity.Property(e => e.payment_frequency).HasMaxLength(50);
            entity.Property(e => e.type).HasMaxLength(50);

            entity.HasOne(d => d.currency_codeNavigation).WithMany(p => p.SalaryType)
                .HasForeignKey(d => d.currency_code)
                .HasConstraintName("FK_SalaryType_Currency");
        });

        modelBuilder.Entity<ShiftAssignment>(entity =>
        {
            entity.HasKey(e => e.assignment_id).HasName("PK__ShiftAss__DA891814749A4BAE");

            entity.Property(e => e.assigned_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.status).HasMaxLength(50);

            entity.HasOne(d => d.employee).WithMany(p => p.ShiftAssignment)
                .HasForeignKey(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShiftAssignment_Employee");

            entity.HasOne(d => d.shift).WithMany(p => p.ShiftAssignment)
                .HasForeignKey(d => d.shift_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShiftAssignment_Shift");
        });

        modelBuilder.Entity<ShiftCycle>(entity =>
        {
            entity.HasKey(e => e.cycle_id).HasName("PK__ShiftCyc__5D955881D20AC745");

            entity.Property(e => e.cycle_name).HasMaxLength(100);
        });

        modelBuilder.Entity<ShiftCycleAssignment>(entity =>
        {
            entity.HasKey(e => new { e.cycle_id, e.shift_id });

            entity.HasOne(d => d.cycle).WithMany(p => p.ShiftCycleAssignment)
                .HasForeignKey(d => d.cycle_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShiftCycleAssignment_Cycle");

            entity.HasOne(d => d.shift).WithMany(p => p.ShiftCycleAssignment)
                .HasForeignKey(d => d.shift_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShiftCycleAssignment_Shift");
        });

        modelBuilder.Entity<ShiftSchedule>(entity =>
        {
            entity.HasKey(e => e.shift_id).HasName("PK__ShiftSch__7B26722013E7191F");

            entity.Property(e => e.name).HasMaxLength(100);
            entity.Property(e => e.status).HasMaxLength(50);
            entity.Property(e => e.type).HasMaxLength(50);
        });

        modelBuilder.Entity<SickLeave>(entity =>
        {
            entity.HasKey(e => e.leave_id).HasName("PK__SickLeav__743350BCC0527195");

            entity.Property(e => e.leave_id).ValueGeneratedNever();

            entity.HasOne(d => d.leave).WithOne(p => p.SickLeave)
                .HasForeignKey<SickLeave>(d => d.leave_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SickLeave_Leave");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.skill_id).HasName("PK__Skill__FBBA83796D2D2C0E");

            entity.Property(e => e.skill_name).HasMaxLength(100);
        });

        modelBuilder.Entity<SystemAdministrator>(entity =>
        {
            entity.HasKey(e => e.employee_id).HasName("PK__SystemAd__C52E0BA8BEE2BC99");

            entity.Property(e => e.employee_id).ValueGeneratedNever();
            entity.Property(e => e.audit_visibility_scope).HasMaxLength(200);
            entity.Property(e => e.password_hash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.employee).WithOne(p => p.SystemAdministrator)
                .HasForeignKey<SystemAdministrator>(d => d.employee_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SystemAdministrator_Employee");
        });

        modelBuilder.Entity<TaxForm>(entity =>
        {
            entity.HasKey(e => e.tax_form_id).HasName("PK__TaxForm__3184195A4E6CEFB0");

            entity.Property(e => e.jurisdiction).HasMaxLength(100);
            entity.Property(e => e.validity_period).HasMaxLength(100);
        });

        modelBuilder.Entity<Termination>(entity =>
        {
            entity.HasKey(e => e.termination_id).HasName("PK__Terminat__B66BAA112803DC47");

            entity.HasOne(d => d.contract).WithMany(p => p.Termination)
                .HasForeignKey(d => d.contract_id)
                .HasConstraintName("FK_Termination_Contract");
        });

        modelBuilder.Entity<VacationLeave>(entity =>
        {
            entity.HasKey(e => e.leave_id).HasName("PK__Vacation__743350BC5E598A20");

            entity.Property(e => e.leave_id).ValueGeneratedNever();

            entity.HasOne(d => d.leave).WithOne(p => p.VacationLeave)
                .HasForeignKey<VacationLeave>(d => d.leave_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VacationLeave_Leave");
        });

        modelBuilder.Entity<Verification>(entity =>
        {
            entity.HasKey(e => e.verification_id).HasName("PK__Verifica__24F1796964813C79");

            entity.Property(e => e.issuer).HasMaxLength(200);
            entity.Property(e => e.verification_type).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

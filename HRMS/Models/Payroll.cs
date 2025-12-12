using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Payroll
{
    public int payroll_id { get; set; }

    public int employee_id { get; set; }

    public decimal? taxes { get; set; }

    public DateOnly? period_start { get; set; }

    public DateOnly? period_end { get; set; }

    public decimal? base_amount { get; set; }

    public decimal? adjustments { get; set; }

    public decimal? contributions { get; set; }

    public decimal? actual_pay { get; set; }

    public decimal? net_salary { get; set; }

    public DateOnly? payment_date { get; set; }

    public virtual ICollection<AllowanceDeduction> AllowanceDeduction { get; set; } = new List<AllowanceDeduction>();

    public virtual ICollection<PayrollPeriod> PayrollPeriod { get; set; } = new List<PayrollPeriod>();

    public virtual ICollection<PayrollPolicy_ID> PayrollPolicy_ID { get; set; } = new List<PayrollPolicy_ID>();

    public virtual ICollection<Payroll_Log> Payroll_Log { get; set; } = new List<Payroll_Log>();

    public virtual Employee employee { get; set; } = null!;
}

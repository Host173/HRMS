using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class PayrollPeriod
{
    public int payroll_period_id { get; set; }

    public int payroll_id { get; set; }

    public DateOnly? start_date { get; set; }

    public DateOnly? end_date { get; set; }

    public string? status { get; set; }

    public virtual Payroll payroll { get; set; } = null!;
}

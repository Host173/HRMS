using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class AllowanceDeduction
{
    public int ad_id { get; set; }

    public int payroll_id { get; set; }

    public int employee_id { get; set; }

    public string? type { get; set; }

    public decimal? amount { get; set; }

    public string? currency_code { get; set; }

    public string? duration { get; set; }

    public string? timezone { get; set; }

    public virtual Currency? currency_codeNavigation { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Payroll payroll { get; set; } = null!;
}

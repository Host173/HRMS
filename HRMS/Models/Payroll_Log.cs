using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Payroll_Log
{
    public int payroll_log_id { get; set; }

    public int payroll_id { get; set; }

    public int? actor { get; set; }

    public DateTime? change_date { get; set; }

    public string? modification_type { get; set; }

    public virtual Employee? actorNavigation { get; set; }

    public virtual Payroll payroll { get; set; } = null!;
}

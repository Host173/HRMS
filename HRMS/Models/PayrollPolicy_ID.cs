using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class PayrollPolicy_ID
{
    public int payroll_policy_id { get; set; }

    public int payroll_id { get; set; }

    public int policy_id { get; set; }

    public virtual Payroll payroll { get; set; } = null!;

    public virtual PayrollPolicy policy { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class LatenessPolicy
{
    public int policy_id { get; set; }

    public int? grace_period_mins { get; set; }

    public decimal? deduction_rate { get; set; }

    public virtual PayrollPolicy policy { get; set; } = null!;
}

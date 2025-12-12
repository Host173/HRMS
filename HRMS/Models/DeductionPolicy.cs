using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class DeductionPolicy
{
    public int policy_id { get; set; }

    public string? deduction_reason { get; set; }

    public string? calculation_mode { get; set; }

    public virtual PayrollPolicy policy { get; set; } = null!;
}

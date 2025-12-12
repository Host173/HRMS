using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class OvertimePolicy
{
    public int policy_id { get; set; }

    public decimal? weekday_rate_multiplier { get; set; }

    public decimal? weekend_rate_multiplier { get; set; }

    public int? max_hours_per_month { get; set; }

    public virtual PayrollPolicy policy { get; set; } = null!;
}

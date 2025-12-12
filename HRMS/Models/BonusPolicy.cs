using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class BonusPolicy
{
    public int policy_id { get; set; }

    public string? bonus_type { get; set; }

    public string? eligibility_criteria { get; set; }

    public virtual PayrollPolicy policy { get; set; } = null!;
}

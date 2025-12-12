using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class LeavePolicy
{
    public int policy_id { get; set; }

    public string name { get; set; } = null!;

    public string? purpose { get; set; }

    public string? eligibility_rules { get; set; }

    public int? notice_period { get; set; }

    public string? special_leave_type { get; set; }

    public bool? reset_on_new_year { get; set; }
}

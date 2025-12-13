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

    // Added to match existing DB columns (see SQL_ADD_LEAVE_POLICY_COLUMNS.sql)
    public int? leave_type_id { get; set; }

    public string? documentation_requirements { get; set; }

    public string? approval_workflow { get; set; }

    public bool? is_active { get; set; }

    public bool? requires_hr_admin_approval { get; set; }

    public int? max_days_per_request { get; set; }

    public int? min_days_per_request { get; set; }

    public bool? requires_documentation { get; set; }

    // Navigation to Leave type (used by LeavePolicyController via Include(p => p.leave_type))
    public virtual Leave? leave_type { get; set; }
}

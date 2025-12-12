using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class FullTimeContract
{
    public int contract_id { get; set; }

    public int? leave_entitlement { get; set; }

    public bool? insurance_eligibility { get; set; }

    public int? weekly_working_hours { get; set; }

    public virtual Contract contract { get; set; } = null!;
}

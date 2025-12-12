using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class LeaveEntitlement
{
    public int leave_entitlement_id { get; set; }

    public int employee_id { get; set; }

    public int leave_type_id { get; set; }

    public decimal? entitlement { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Leave leave_type { get; set; } = null!;
}

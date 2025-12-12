using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class VacationLeave
{
    public int leave_id { get; set; }

    public int? carry_over_days { get; set; }

    public int? approving_manager { get; set; }

    public virtual Leave leave { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class ProbationLeave
{
    public int leave_id { get; set; }

    public DateOnly? eligibility_start_date { get; set; }

    public int? probation_period { get; set; }

    public virtual Leave leave { get; set; } = null!;
}

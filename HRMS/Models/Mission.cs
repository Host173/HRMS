using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Mission
{
    public int mission_id { get; set; }

    public string? destination { get; set; }

    public DateOnly? start_date { get; set; }

    public DateOnly? end_date { get; set; }

    public string? status { get; set; }

    public int employee_id { get; set; }

    public int? manager_id { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Employee? manager { get; set; }
}

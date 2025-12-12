using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class LineManager
{
    public int employee_id { get; set; }

    public int? team_size { get; set; }

    public string? supervised_departments { get; set; }

    public decimal? approval_limit { get; set; }

    public virtual Employee employee { get; set; } = null!;
}

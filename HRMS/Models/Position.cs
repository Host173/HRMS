using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Position
{
    public int position_id { get; set; }

    public string position_title { get; set; } = null!;

    public string? responsibilities { get; set; }

    public string? status { get; set; }

    public virtual ICollection<Employee> Employee { get; set; } = new List<Employee>();
}

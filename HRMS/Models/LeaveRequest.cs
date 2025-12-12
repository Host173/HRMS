using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class LeaveRequest
{
    public int request_id { get; set; }

    public int employee_id { get; set; }

    public int leave_id { get; set; }

    public string? justification { get; set; }

    public int duration { get; set; }

    public string? approval_timing { get; set; }

    public string status { get; set; } = null!;

    public int? approved_by { get; set; }

    public virtual ICollection<LeaveDocument> LeaveDocument { get; set; } = new List<LeaveDocument>();

    public virtual Employee employee { get; set; } = null!;

    public virtual Leave leave { get; set; } = null!;
}

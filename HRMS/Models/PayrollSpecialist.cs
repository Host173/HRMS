using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class PayrollSpecialist
{
    public int employee_id { get; set; }

    public string? assigned_region { get; set; }

    public string? processing_frequency { get; set; }

    public DateOnly? last_processed_period { get; set; }

    public virtual Employee employee { get; set; } = null!;
}

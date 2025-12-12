using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class AttendanceCorrectionRequest
{
    public int attendance_correction_request_id { get; set; }

    public int employee_id { get; set; }

    public DateOnly date { get; set; }

    public string? correction_type { get; set; }

    public string? reason { get; set; }

    public string? status { get; set; }

    public int? recorded_by { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Employee? recorded_byNavigation { get; set; }
}

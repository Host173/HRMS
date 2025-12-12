using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class AttendanceLog
{
    public int attendance_log_id { get; set; }

    public int attendance_id { get; set; }

    public int? actor { get; set; }

    public DateTime? timestamp { get; set; }

    public string? reason { get; set; }

    public virtual Attendance attendance { get; set; } = null!;
}

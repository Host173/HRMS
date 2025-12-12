using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class AttendanceSource
{
    public int attendance_id { get; set; }

    public int device_id { get; set; }

    public string? source_type { get; set; }

    public decimal? latitude { get; set; }

    public decimal? longitude { get; set; }

    public DateTime? recorded_at { get; set; }

    public virtual Attendance attendance { get; set; } = null!;

    public virtual Device device { get; set; } = null!;
}

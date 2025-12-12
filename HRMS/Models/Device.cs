using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Device
{
    public int device_id { get; set; }

    public string? device_type { get; set; }

    public string? terminal_id { get; set; }

    public decimal? latitude { get; set; }

    public decimal? longitude { get; set; }

    public int? employee_id { get; set; }

    public virtual ICollection<AttendanceSource> AttendanceSource { get; set; } = new List<AttendanceSource>();

    public virtual Employee? employee { get; set; }
}

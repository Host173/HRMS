using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Attendance
{
    public int attendance_id { get; set; }

    public int employee_id { get; set; }

    public int? shift_id { get; set; }

    public DateTime? entry_time { get; set; }

    public DateTime? exit_time { get; set; }

    public decimal? duration { get; set; }

    public string? login_method { get; set; }

    public string? logout_method { get; set; }

    public int? exception_id { get; set; }

    public virtual ICollection<AttendanceLog> AttendanceLog { get; set; } = new List<AttendanceLog>();

    public virtual ICollection<AttendanceSource> AttendanceSource { get; set; } = new List<AttendanceSource>();

    public virtual Employee employee { get; set; } = null!;

    public virtual Exception? exception { get; set; }

    public virtual ShiftSchedule? shift { get; set; }
}

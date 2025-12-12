using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class ShiftSchedule
{
    public int shift_id { get; set; }

    public string name { get; set; } = null!;

    public string? type { get; set; }

    public TimeOnly? start_time { get; set; }

    public TimeOnly? end_time { get; set; }

    public int? break_duration { get; set; }

    public DateOnly? shift_date { get; set; }

    public string? status { get; set; }

    public virtual ICollection<Attendance> Attendance { get; set; } = new List<Attendance>();

    public virtual ICollection<ShiftAssignment> ShiftAssignment { get; set; } = new List<ShiftAssignment>();

    public virtual ICollection<ShiftCycleAssignment> ShiftCycleAssignment { get; set; } = new List<ShiftCycleAssignment>();
}

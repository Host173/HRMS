using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class ShiftCycleAssignment
{
    public int cycle_id { get; set; }

    public int shift_id { get; set; }

    public int? order_number { get; set; }

    public virtual ShiftCycle cycle { get; set; } = null!;

    public virtual ShiftSchedule shift { get; set; } = null!;
}

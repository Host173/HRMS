using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class ShiftCycle
{
    public int cycle_id { get; set; }

    public string cycle_name { get; set; } = null!;

    public string? description { get; set; }

    public virtual ICollection<ShiftCycleAssignment> ShiftCycleAssignment { get; set; } = new List<ShiftCycleAssignment>();
}

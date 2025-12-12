using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Leave
{
    public int leave_id { get; set; }

    public string leave_type { get; set; } = null!;

    public string? leave_description { get; set; }

    public virtual HolidayLeave? HolidayLeave { get; set; }

    public virtual ICollection<LeaveEntitlement> LeaveEntitlement { get; set; } = new List<LeaveEntitlement>();

    public virtual ICollection<LeaveRequest> LeaveRequest { get; set; } = new List<LeaveRequest>();

    public virtual ProbationLeave? ProbationLeave { get; set; }

    public virtual SickLeave? SickLeave { get; set; }

    public virtual VacationLeave? VacationLeave { get; set; }
}

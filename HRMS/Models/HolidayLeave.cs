using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class HolidayLeave
{
    public int leave_id { get; set; }

    public string? holiday_name { get; set; }

    public bool? official_recognition { get; set; }

    public string? regional_scope { get; set; }

    public virtual Leave leave { get; set; } = null!;
}

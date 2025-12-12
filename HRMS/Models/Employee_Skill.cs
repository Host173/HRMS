using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Employee_Skill
{
    public int employee_id { get; set; }

    public int skill_id { get; set; }

    public string? proficiency_level { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Skill skill { get; set; } = null!;
}

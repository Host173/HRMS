using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Employee_Role
{
    public int employee_id { get; set; }

    public int role_id { get; set; }

    public DateOnly? assigned_date { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Role role { get; set; } = null!;
}

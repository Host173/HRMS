using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class EmployeeHierarchy
{
    public int employee_hierarchy_id { get; set; }

    public int employee_id { get; set; }

    public int? manager_id { get; set; }

    public int? hierarchy_level { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Employee? manager { get; set; }
}

using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class SystemAdministrator
{
    public int employee_id { get; set; }

    public int? system_privilege_level { get; set; }

    public string? configurable_fields { get; set; }

    public string? audit_visibility_scope { get; set; }

    public string? password_hash { get; set; }

    public virtual Employee employee { get; set; } = null!;
}

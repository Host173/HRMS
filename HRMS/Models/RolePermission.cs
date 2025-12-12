using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class RolePermission
{
    public int role_id { get; set; }

    public string permission_name { get; set; } = null!;

    public string? allowed_action { get; set; }

    public virtual Role role { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Role
{
    public int role_id { get; set; }

    public string role_name { get; set; } = null!;

    public string? purpose { get; set; }

    public virtual ICollection<ApprovalWorkflowStep> ApprovalWorkflowStep { get; set; } = new List<ApprovalWorkflowStep>();

    public virtual ICollection<Employee_Role> Employee_Role { get; set; } = new List<Employee_Role>();

    public virtual ICollection<RolePermission> RolePermission { get; set; } = new List<RolePermission>();
}

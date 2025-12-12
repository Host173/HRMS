using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class ApprovalWorkflowStep
{
    public int approval_workflow_step_id { get; set; }

    public int workflow_id { get; set; }

    public int step_number { get; set; }

    public int role_id { get; set; }

    public string? action_required { get; set; }

    public virtual Role role { get; set; } = null!;

    public virtual ApprovalWorkflow workflow { get; set; } = null!;
}

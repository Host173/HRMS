using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class ApprovalWorkflow
{
    public int workflow_id { get; set; }

    public string? workflow_type { get; set; }

    public decimal? threshold_amount { get; set; }

    public string? approver_role { get; set; }

    public int? created_by { get; set; }

    public string? status { get; set; }

    public virtual ICollection<ApprovalWorkflowStep> ApprovalWorkflowStep { get; set; } = new List<ApprovalWorkflowStep>();
}

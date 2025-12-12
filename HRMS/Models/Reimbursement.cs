using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Reimbursement
{
    public int reimbursement_id { get; set; }

    public string? type { get; set; }

    public string? claim_type { get; set; }

    public DateOnly? approval_date { get; set; }

    public string? current_status { get; set; }

    public int employee_id { get; set; }

    public virtual Employee employee { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class PayrollPolicy
{
    public int policy_id { get; set; }

    public DateOnly? effective_date { get; set; }

    public string? type { get; set; }

    public string? description { get; set; }

    public virtual BonusPolicy? BonusPolicy { get; set; }

    public virtual DeductionPolicy? DeductionPolicy { get; set; }

    public virtual LatenessPolicy? LatenessPolicy { get; set; }

    public virtual OvertimePolicy? OvertimePolicy { get; set; }

    public virtual ICollection<PayrollPolicy_ID> PayrollPolicy_ID { get; set; } = new List<PayrollPolicy_ID>();
}

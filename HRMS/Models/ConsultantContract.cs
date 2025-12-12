using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class ConsultantContract
{
    public int contract_id { get; set; }

    public string? project_scope { get; set; }

    public decimal? fees { get; set; }

    public string? payment_schedule { get; set; }

    public virtual Contract contract { get; set; } = null!;
}

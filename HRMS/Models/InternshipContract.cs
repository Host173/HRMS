using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class InternshipContract
{
    public int contract_id { get; set; }

    public string? mentoring { get; set; }

    public string? evaluation { get; set; }

    public string? stipend_related { get; set; }

    public virtual Contract contract { get; set; } = null!;
}

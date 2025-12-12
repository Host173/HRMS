using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Verification
{
    public int verification_id { get; set; }

    public string verification_type { get; set; } = null!;

    public string? issuer { get; set; }

    public DateOnly? issue_date { get; set; }

    public int? expiry_period { get; set; }

    public virtual ICollection<Employee> employee { get; set; } = new List<Employee>();
}

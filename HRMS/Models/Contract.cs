using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Contract
{
    public int contract_id { get; set; }

    public string type { get; set; } = null!;

    public DateOnly? start_date { get; set; }

    public DateOnly? end_date { get; set; }

    public string? current_state { get; set; }

    public virtual ConsultantContract? ConsultantContract { get; set; }

    public virtual ICollection<Employee> Employee { get; set; } = new List<Employee>();

    public virtual FullTimeContract? FullTimeContract { get; set; }

    public virtual InternshipContract? InternshipContract { get; set; }

    public virtual PartTimeContract? PartTimeContract { get; set; }

    public virtual ICollection<Termination> Termination { get; set; } = new List<Termination>();
}

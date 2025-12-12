using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class SalaryType
{
    public int salary_type_id { get; set; }

    public string type { get; set; } = null!;

    public string? payment_frequency { get; set; }

    public string? currency_code { get; set; }

    public DateOnly? effective_date { get; set; }

    public virtual ICollection<Employee> Employee { get; set; } = new List<Employee>();

    public virtual Currency? currency_codeNavigation { get; set; }
}

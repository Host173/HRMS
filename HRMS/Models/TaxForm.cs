using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class TaxForm
{
    public int tax_form_id { get; set; }

    public string jurisdiction { get; set; } = null!;

    public string? validity_period { get; set; }

    public string? form_content { get; set; }

    public virtual ICollection<Employee> Employee { get; set; } = new List<Employee>();
}

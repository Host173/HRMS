using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class PayGrade
{
    public int pay_grade_id { get; set; }

    public string grade_name { get; set; } = null!;

    public decimal? min_salary { get; set; }

    public decimal? max_salary { get; set; }

    public virtual ICollection<Employee> Employee { get; set; } = new List<Employee>();
}

using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Exception
{
    public int exception_id { get; set; }

    public string name { get; set; } = null!;

    public string? category { get; set; }

    public DateOnly? date { get; set; }

    public string? status { get; set; }

    public virtual ICollection<Attendance> Attendance { get; set; } = new List<Attendance>();

    public virtual ICollection<Employee> employee { get; set; } = new List<Employee>();
}

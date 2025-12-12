using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class ManagerNotes
{
    public int note_id { get; set; }

    public int employee_id { get; set; }

    public int manager_id { get; set; }

    public string? note_content { get; set; }

    public DateTime? created_at { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Employee manager { get; set; } = null!;
}

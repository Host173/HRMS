using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class HRAdministrator
{
    public int employee_id { get; set; }

    public int? approval_level { get; set; }

    public string? record_access_scope { get; set; }

    public bool? document_validation_rights { get; set; }

    public string? password_hash { get; set; }

    public virtual Employee employee { get; set; } = null!;
}

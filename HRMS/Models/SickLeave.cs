using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class SickLeave
{
    public int leave_id { get; set; }

    public bool? medical_cert_required { get; set; }

    public int? physician_id { get; set; }

    public virtual Leave leave { get; set; } = null!;
}

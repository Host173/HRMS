using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class LeaveDocument
{
    public int document_id { get; set; }

    public int leave_request_id { get; set; }

    public string? file_path { get; set; }

    public DateTime? uploaded_at { get; set; }

    public virtual LeaveRequest leave_request { get; set; } = null!;
}

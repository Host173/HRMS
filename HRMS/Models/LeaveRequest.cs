using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class LeaveRequest
{
    public int request_id { get; set; }

    public int employee_id { get; set; }

    public int leave_id { get; set; }

    // Added to match existing DB columns (see SQL_ADD_LEAVE_REQUEST_COLUMNS.sql / SQL_ADD_IRREGULARITY_REASON.sql)
    // Nullable because the DB migration adds them as nullable for backwards compatibility.
    public DateOnly? start_date { get; set; }

    public DateOnly? end_date { get; set; }

    public string? justification { get; set; }

    public int duration { get; set; }

    public string? approval_timing { get; set; }

    public string status { get; set; } = null!;

    public int? approved_by { get; set; }

    // Added to match existing DB columns (BIT NOT NULL DEFAULT 0 / NVARCHAR(MAX) NULL).
    public bool is_irregular { get; set; }

    public string? irregularity_reason { get; set; }

    // Added to match existing DB column (DATETIME NULL).
    public DateTime? created_at { get; set; }

    public virtual ICollection<LeaveDocument> LeaveDocument { get; set; } = new List<LeaveDocument>();

    public virtual Employee employee { get; set; } = null!;

    public virtual Leave leave { get; set; } = null!;
}

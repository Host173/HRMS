namespace HRMS.Models;

public class LeaveRequestViewModel
{
    public int RequestId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Duration { get; set; }
    public string? Justification { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public bool IsIrregular { get; set; }
    public string? IrregularityReason { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsSpecialLeave { get; set; }
    public bool ApprovedByHR { get; set; }
    public List<LeaveDocumentViewModel> Documents { get; set; } = new();
}

public class LeaveDocumentViewModel
{
    public int DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime? UploadedAt { get; set; }
}

public class LeaveBalanceViewModel
{
    public string LeaveType { get; set; } = string.Empty;
    public decimal TotalEntitlement { get; set; }
    public decimal Used { get; set; }
    public decimal Remaining { get; set; }
}


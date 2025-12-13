using System.ComponentModel.DataAnnotations;

namespace HRMS.Models;

public class LeaveTypeViewModel
{
    public int LeaveId { get; set; }
    
    [Required(ErrorMessage = "Leave type name is required")]
    [Display(Name = "Leave Type Name")]
    [StringLength(100)]
    public string LeaveType { get; set; } = string.Empty;
    
    [Display(Name = "Description")]
    [StringLength(500)]
    public string? LeaveDescription { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsSpecialLeave { get; set; }
    
    public int RequestCount { get; set; }
}

public class LeavePolicyViewModel
{
    public int PolicyId { get; set; }
    
    [Required(ErrorMessage = "Policy name is required")]
    [Display(Name = "Policy Name")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    
    [Display(Name = "Purpose")]
    [StringLength(1000)]
    public string? Purpose { get; set; }
    
    [Display(Name = "Eligibility Rules")]
    [StringLength(2000)]
    public string? EligibilityRules { get; set; }
    
    [Display(Name = "Documentation Requirements")]
    [StringLength(2000)]
    public string? DocumentationRequirements { get; set; }
    
    [Display(Name = "Approval Workflow")]
    [StringLength(500)]
    public string? ApprovalWorkflow { get; set; }
    
    [Display(Name = "Notice Period (days)")]
    public int? NoticePeriod { get; set; }
    
    [Display(Name = "Special Leave Type")]
    [StringLength(100)]
    public string? SpecialLeaveType { get; set; }
    
    [Display(Name = "Reset on New Year")]
    public bool ResetOnNewYear { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Requires HR Admin Approval Only")]
    public bool RequiresHRAdminApproval { get; set; }
    
    [Display(Name = "Maximum Days Per Request")]
    public int? MaxDaysPerRequest { get; set; }
    
    [Display(Name = "Minimum Days Per Request")]
    public int? MinDaysPerRequest { get; set; }
    
    [Display(Name = "Requires Documentation")]
    public bool RequiresDocumentation { get; set; }
}

public class LeaveEntitlementAdjustmentViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Leave Type")]
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Entitlement (days)")]
    [Range(0, 365, ErrorMessage = "Entitlement must be between 0 and 365 days")]
    public decimal Entitlement { get; set; }
    
    [Display(Name = "Reason for Adjustment")]
    [StringLength(500)]
    public string? Reason { get; set; }
}



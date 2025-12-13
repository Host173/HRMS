using System.ComponentModel.DataAnnotations;

namespace HRMS.Models;

public class CreateLeaveRequestViewModel
{
    [Required(ErrorMessage = "Leave type is required")]
    [Display(Name = "Leave Type")]
    public int LeaveTypeId { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    [Display(Name = "Justification/Reason")]
    [StringLength(1000)]
    public string? Justification { get; set; }

    [Display(Name = "Attachments")]
    public List<IFormFile>? Attachments { get; set; }

    // For dropdown
    public List<Leave>? AvailableLeaveTypes { get; set; }
}


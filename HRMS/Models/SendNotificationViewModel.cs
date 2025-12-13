using System.ComponentModel.DataAnnotations;

namespace HRMS.Models;

public class SendNotificationViewModel
{
    [Required(ErrorMessage = "Message is required")]
    [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
    public string Message { get; set; } = string.Empty;

    public string NotificationType { get; set; } = "General";

    public string Urgency { get; set; } = "Normal";

    public bool SendToTeam { get; set; } = false;

    public int? ReceiverId { get; set; }

    public int? DepartmentId { get; set; }
}


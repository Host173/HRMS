namespace HRMS.Models;

public class NotificationViewModel
{
    public int NotificationId { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? NotificationType { get; set; }
    public string? Urgency { get; set; }
}


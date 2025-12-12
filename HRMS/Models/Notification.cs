using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Notification
{
    public int notification_id { get; set; }

    public string? message_content { get; set; }

    public DateTime? timestamp { get; set; }

    public string? urgency { get; set; }

    public bool? read_status { get; set; }

    public string? notification_type { get; set; }

    public virtual ICollection<Employee_Notification> Employee_Notification { get; set; } = new List<Employee_Notification>();
}

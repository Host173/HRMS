using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Employee_Notification
{
    public int employee_notification_id { get; set; }

    public int employee_id { get; set; }

    public int notification_id { get; set; }

    public string? delivery_status { get; set; }

    public DateTime? delivered_at { get; set; }

    public virtual Employee employee { get; set; } = null!;

    public virtual Notification notification { get; set; } = null!;
}

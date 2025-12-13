using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

public class NotificationService : INotificationService
{
    private readonly HrmsDbContext _context;

    public NotificationService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(int notificationId)
    {
        return await _context.Notification
            .Include(n => n.Employee_Notification)
            .FirstOrDefaultAsync(n => n.notification_id == notificationId);
    }

    public async Task<IEnumerable<Notification>> GetAllAsync()
    {
        return await _context.Notification
            .OrderByDescending(n => n.timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Notification
            .Where(n => n.Employee_Notification.Any(en => en.employee_id == employeeId))
            .OrderByDescending(n => n.timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetUnreadByEmployeeIdAsync(int employeeId)
    {
        return await _context.Notification
            .Where(n => n.Employee_Notification.Any(en => 
                en.employee_id == employeeId && 
                (en.delivery_status != "Read" || en.delivery_status == null)))
            .OrderByDescending(n => n.timestamp)
            .ToListAsync();
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notification.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<Notification> UpdateAsync(Notification notification)
    {
        _context.Notification.Update(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<bool> DeleteAsync(int notificationId)
    {
        var notification = await GetByIdAsync(notificationId);
        if (notification == null)
            return false;

        _context.Notification.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int employeeId)
    {
        var employeeNotification = await _context.Employee_Notification
            .FirstOrDefaultAsync(en => en.notification_id == notificationId && en.employee_id == employeeId);
        
        if (employeeNotification == null)
            return false;

        employeeNotification.delivery_status = "Read";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int employeeId)
    {
        var employeeNotifications = await _context.Employee_Notification
            .Where(en => en.employee_id == employeeId && 
                        (en.delivery_status != "Read" || en.delivery_status == null))
            .ToListAsync();

        foreach (var en in employeeNotifications)
        {
            en.delivery_status = "Read";
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int notificationId)
    {
        return await _context.Notification
            .AnyAsync(n => n.notification_id == notificationId);
    }

    /// <summary>
    /// Creates a notification and links it to an employee
    /// </summary>
    public async Task<Notification> CreateNotificationAsync(
        int employeeId, 
        string title, 
        string message, 
        string notificationType = "General",
        string urgency = "Normal")
    {
        var notification = new Notification
        {
            notification_type = notificationType,
            message_content = $"{title}: {message}",
            urgency = urgency,
            timestamp = DateTime.UtcNow,
            read_status = false
        };

        _context.Notification.Add(notification);
        await _context.SaveChangesAsync();

        // Link notification to employee
        var employeeNotification = new Employee_Notification
        {
            employee_id = employeeId,
            notification_id = notification.notification_id,
            delivery_status = "Unread"
        };

        _context.Employee_Notification.Add(employeeNotification);
        await _context.SaveChangesAsync();

        return notification;
    }
}


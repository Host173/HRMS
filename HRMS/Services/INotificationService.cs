using HRMS.Models;

namespace HRMS.Services;

public interface INotificationService
{
    Task<Notification?> GetByIdAsync(int notificationId);
    Task<IEnumerable<Notification>> GetAllAsync();
    Task<IEnumerable<Notification>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Notification>> GetUnreadByEmployeeIdAsync(int employeeId);
    Task<Notification> CreateAsync(Notification notification);
    Task<Notification> UpdateAsync(Notification notification);
    Task<bool> DeleteAsync(int notificationId);
    Task<bool> MarkAsReadAsync(int notificationId, int employeeId);
    Task<bool> MarkAllAsReadAsync(int employeeId);
    Task<bool> ExistsAsync(int notificationId);
}


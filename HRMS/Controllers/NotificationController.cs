using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Data;
using HRMS.Models;
using HRMS.Services;
using HRMS.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        HrmsDbContext context,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Display all notifications for the current employee
    /// </summary>
    public async Task<IActionResult> Index(string filter = "all")
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return Unauthorized();
        }

        IEnumerable<Notification> notifications;

        if (filter == "unread")
        {
            notifications = await _notificationService.GetUnreadByEmployeeIdAsync(currentEmployeeId.Value);
            ViewBag.FilterTitle = "Unread Notifications";
        }
        else
        {
            notifications = await _notificationService.GetByEmployeeIdAsync(currentEmployeeId.Value);
            ViewBag.FilterTitle = "All Notifications";
        }

        // Load delivery status for each notification
        var notificationList = notifications.ToList();
        foreach (var notification in notificationList)
        {
            await _context.Entry(notification)
                .Collection(n => n.Employee_Notification)
                .Query()
                .Where(en => en.employee_id == currentEmployeeId.Value)
                .LoadAsync();
        }

        ViewBag.CurrentFilter = filter;
        ViewBag.UnreadCount = (await _notificationService.GetUnreadByEmployeeIdAsync(currentEmployeeId.Value)).Count();

        return View(notificationList);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _notificationService.MarkAsReadAsync(id, currentEmployeeId.Value);
        
        if (success)
        {
            _logger.LogInformation("Notification {NotificationId} marked as read by employee {EmployeeId}", 
                id, currentEmployeeId.Value);
        }

        // Return JSON for AJAX requests
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success });
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return Unauthorized();
        }

        await _notificationService.MarkAllAsReadAsync(currentEmployeeId.Value);
        
        _logger.LogInformation("All notifications marked as read by employee {EmployeeId}", currentEmployeeId.Value);
        
        TempData["SuccessMessage"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Get unread notification count (for navbar badge)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return Json(new { count = 0 });
        }

        var unreadNotifications = await _notificationService.GetUnreadByEmployeeIdAsync(currentEmployeeId.Value);
        var count = unreadNotifications.Count();

        return Json(new { count });
    }

    /// <summary>
    /// View notification details
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return Unauthorized();
        }

        var notification = await _notificationService.GetByIdAsync(id);
        if (notification == null)
        {
            return NotFound();
        }

        // Verify this notification belongs to the current employee
        await _context.Entry(notification)
            .Collection(n => n.Employee_Notification)
            .LoadAsync();

        var employeeNotification = notification.Employee_Notification
            .FirstOrDefault(en => en.employee_id == currentEmployeeId.Value);

        if (employeeNotification == null)
        {
            return Forbid();
        }

        // Mark as read when viewing details
        await _notificationService.MarkAsReadAsync(id, currentEmployeeId.Value);

        return View(notification);
    }
}




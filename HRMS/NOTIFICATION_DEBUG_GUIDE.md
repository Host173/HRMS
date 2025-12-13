# Notification System Debug Guide

## How to Test Notifications

### Step 1: Send a Notification as Line Manager
1. Log in as a Line Manager account
2. Navigate to "Send Notification" (from Dashboard or Notifications page)
3. **Uncheck** "Send to my entire team"
4. Select a specific team member from the dropdown
5. Enter a message
6. Click "Send Notification"

### Step 2: Verify Notification was Created
Check the application logs (Rider Output window) for:
- `Notification created with ID: X`
- `Added Employee_Notification link: NotificationId=X, EmployeeId=Y`
- `Verification: Found X Employee_Notification links`

### Step 3: View as Recipient Employee
1. Log out and log in as the employee you sent the notification to
2. Click "Notifications" in the navigation bar
3. The notification should appear

## Debug Endpoint

If notifications still don't appear, use the debug endpoint:

**URL:** `/Component5/DebugNotifications?employeeId=YOUR_EMPLOYEE_ID`

This will show:
- All notifications in the database
- All Employee_Notification links
- Which employees are linked to which notifications
- Total counts

## Common Issues

### Issue 1: Notification not appearing
**Check:**
- Application logs for errors
- Database directly: `SELECT * FROM Employee_Notification WHERE employee_id = X`
- Database: `SELECT * FROM Notification WHERE notification_id = Y`

### Issue 2: Links not being created
**Check logs for:**
- "No Employee_Notification links were created!"
- Any SaveChanges errors
- Verification counts

### Issue 3: Query not finding notifications
**Check:**
- Employee ID is correct
- Employee_Notification table has the correct employee_id
- Notification table has the notification records

## Database Queries for Manual Verification

```sql
-- Check all notifications
SELECT * FROM Notification ORDER BY timestamp DESC;

-- Check all employee-notification links
SELECT * FROM Employee_Notification;

-- Check notifications for specific employee
SELECT n.*, en.delivery_status, en.delivered_at
FROM Notification n
INNER JOIN Employee_Notification en ON n.notification_id = en.notification_id
WHERE en.employee_id = YOUR_EMPLOYEE_ID
ORDER BY n.timestamp DESC;
```

## Files Modified

1. **Component5Controller.cs** - Main notification logic
2. **SendNotification.cshtml** - Form with validation
3. **MyNotifications.cshtml** - Display notifications
4. **NotificationViewModel.cs** - View model
5. **SendNotificationViewModel.cs** - Form model

## Key Changes Made

1. Changed query to use `Employee_Notification` directly instead of filtering `Notification`
2. Added extensive logging throughout the process
3. Added verification after saving links
4. Added null checks and error handling
5. Added debug endpoint for troubleshooting


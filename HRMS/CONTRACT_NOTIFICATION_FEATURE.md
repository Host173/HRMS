# 📧 Contract Notification System

## Overview

The HRMS system now includes a **comprehensive contract notification system** that automatically notifies employees whenever their employment contracts are created or updated by HR Admins. This ensures transparency and keeps employees informed about important changes to their employment terms.

---

## ✨ Features

### 1. Automatic Notifications

#### Contract Creation
When an HR Admin creates a new contract, the system automatically:
- ✅ Sends a detailed notification to the employee
- 📝 Includes contract type, start date, end date (if applicable), and status
- 🔔 Marks the notification as "Normal" priority

**Sample Notification:**
> **New Contract Created**
> 
> A new employment contract has been created for you by HR Admin. Details: Contract Type: Full-Time; Start Date: Jan 01, 2025; Status: Active; End Date: Dec 31, 2025. Please review your contract details in the system.

#### Contract Updates
When an HR Admin updates an existing contract, the system:
- ✅ Tracks all changes (status, type, start date, end date)
- 📊 Sends a detailed notification listing all changes
- ⚠️ Marks terminations as "High" priority (urgent)
- ℹ️ Marks other updates as "Normal" priority

**Sample Notification (Regular Update):**
> **Contract Updated**
> 
> Your employment contract has been updated by HR Admin. Changes: End date changed to Jun 30, 2025.

**Sample Notification (Termination):**
> **Contract Updated** [URGENT]
> 
> Your employment contract has been updated by HR Admin. Changes: Status changed from 'Active' to 'Terminated'.

---

## 🎨 User Interface

### Notification Bell (Navbar)
- **Location:** Top navigation bar, between "Attendance" and user profile
- **Badge:** Shows count of unread notifications
- **Auto-refresh:** Updates every 30 seconds
- **Animation:** Bell rings when new notifications arrive

### Notifications Page
**Access:** Click the bell icon in the navbar

**Features:**
- View all notifications or filter by unread
- Beautiful card-based layout with:
  - Notification type icons (Contract, Leave, Attendance, etc.)
  - Urgency indicators (High = red border, Normal = blue border)
  - Timestamp in local time
  - "New" badge for unread notifications
  - Action buttons to view details or mark as read
- Empty states with helpful messages
- Bulk "Mark All Read" action
- Real-time AJAX for marking notifications as read

### Notification Details Page
**Access:** Click "View Details" on any notification

**Features:**
- Full notification message
- Priority level badge
- Timestamp details
- Related action links (e.g., "View My Contracts")
- Automatically marks notification as read

---

## 🔧 Technical Implementation

### Backend

#### Controllers
1. **ContractController.cs**
   - `Create (POST)`: Sends notification when contract is created
   - `Edit (POST)`: Tracks changes and sends detailed notification when contract is updated
   - Includes change detection for: status, type, start date, end date

2. **NotificationController.cs**
   - `Index`: Display all notifications with filtering
   - `Details`: View individual notification
   - `MarkAsRead`: Mark notification as read (supports AJAX)
   - `MarkAllAsRead`: Bulk mark all as read
   - `GetUnreadCount`: API endpoint for real-time count updates

#### Services
- **NotificationService.cs**: Handles all notification CRUD operations
- **INotificationService.cs**: Interface defining notification service methods

#### Key Methods
```csharp
// Create notification
await _notificationService.CreateNotificationAsync(
    employeeId: 123,
    title: "Contract Updated",
    message: "Your contract has been updated...",
    notificationType: "Contract",
    urgency: "High"
);
```

### Frontend

#### Views
1. **Views/Notification/Index.cshtml**
   - Beautiful notification list with animations
   - Filter by all/unread
   - Real-time AJAX for marking as read
   - Empty states

2. **Views/Notification/Details.cshtml**
   - Full notification details
   - Related action links
   - Professional card layout

3. **Views/Shared/_Layout.cshtml**
   - Notification bell icon in navbar
   - Badge showing unread count

#### JavaScript (enhancements.js)
```javascript
// Automatically fetches and updates notification count
NotificationSystem.init();
- Fetches count on page load
- Auto-refreshes every 30 seconds
- Animates bell on new notifications
```

#### CSS (enhancements.css)
```css
/* Notification badge styles */
.notification-badge
- Position: absolute
- Background: danger red
- Animation: pop-in effect

/* Bell animation */
@keyframes notification-ring
- Rotates bell left-right when new notification arrives
```

---

## 📊 Notification Types

| Type | Icon | Use Case | Example |
|------|------|----------|---------|
| **Contract** | 📄 | Contract creation/updates | "Your contract has been updated" |
| **Leave** | 📅 | Leave approvals/rejections | "Your leave request was approved" |
| **Attendance** | 🕐 | Attendance issues | "Attendance marked for today" |
| **Salary** | 💰 | Salary updates | "Salary processed for this month" |
| **General** | ℹ️ | Other notifications | General system messages |

---

## 🎯 Priority Levels

| Priority | Badge Color | Use Case | Example |
|----------|-------------|----------|---------|
| **High** | 🔴 Red | Urgent actions required | Contract termination |
| **Medium** | 🟡 Yellow | Important but not urgent | Contract expiring soon |
| **Normal** | 🔵 Blue | Standard notifications | Regular contract updates |

---

## 🚀 Usage Guide

### For HR Admins

#### Creating a Contract
1. Navigate to **Contracts > Create**
2. Fill in contract details
3. Select the employee
4. Click **Save**
5. ✅ System automatically sends notification to employee

#### Updating a Contract
1. Navigate to **Contracts > View > Edit**
2. Make changes to the contract
3. Click **Save**
4. ✅ System tracks changes and sends detailed notification
5. ✅ If status changed to "Terminated", notification marked as urgent

### For Employees

#### Viewing Notifications
1. Click the **bell icon** (🔔) in the navbar
2. View unread count badge
3. Click to open notifications page
4. Filter by All or Unread
5. Click **View Details** to see full notification

#### Managing Notifications
- **Mark as Read:** Click "Mark as Read" button on notification card
- **Mark All Read:** Click "Mark All Read" button at the top
- **View Details:** Click "View Details" to see full message and related actions

---

## 🎨 Visual Features

### Animations
- ✨ **Fade-in:** Notifications slide in smoothly
- 🔄 **Bell Ring:** Bell icon rotates when new notifications arrive
- 💫 **Badge Pop:** Count badge pops in with bounce effect
- 📊 **Card Hover:** Cards lift on hover
- 🌊 **Staggered Load:** Cards appear with slight delay for smooth effect

### Responsive Design
- 📱 Mobile-friendly
- 💻 Desktop-optimized
- 🎨 Dark mode compatible
- ♿ Accessible keyboard navigation

---

## 🔐 Security & Permissions

### Authorization
- **Create/Update Contracts:** Only HR Admins
- **View Notifications:** Only the employee who received the notification
- **Mark as Read:** Only the notification owner

### Validation
- Employee verification before sending notifications
- Notification ownership checks in Details view
- CSRF protection on all POST actions

---

## 📈 Future Enhancements

Potential additions for future versions:
- [ ] Email notifications (in addition to in-app)
- [ ] SMS notifications for urgent updates
- [ ] Push notifications (browser/mobile)
- [ ] Notification preferences/settings
- [ ] Digest mode (daily/weekly summary)
- [ ] Notification history archive
- [ ] Export notifications to PDF

---

## 🛠️ Troubleshooting

### Notifications Not Appearing?
1. Check that NotificationService is registered in `Program.cs`
2. Verify database tables: `Notification`, `Employee_Notification`
3. Check browser console for JavaScript errors
4. Ensure employee has valid `employee_id`

### Badge Not Updating?
1. Check browser console for fetch errors
2. Verify `/Notification/GetUnreadCount` endpoint is accessible
3. Clear browser cache and refresh
4. Check if JavaScript is enabled

### Bell Not Animating?
1. Verify `enhancements.css` is loaded
2. Check for CSS conflicts
3. Ensure JavaScript `NotificationSystem.init()` is called

---

## 📝 Database Schema

### Notification Table
```sql
- notification_id (PK)
- notification_type (Contract, Leave, etc.)
- message_content (Full message)
- urgency (High, Medium, Normal)
- timestamp (DateTime)
- read_status (bool)
```

### Employee_Notification Table (Junction)
```sql
- employee_id (FK)
- notification_id (FK)
- delivery_status (Unread, Read)
```

---

## ✅ Testing Checklist

- [x] HR Admin can create contract → Employee receives notification
- [x] HR Admin can update contract → Employee receives notification with changes
- [x] Contract termination marked as High priority
- [x] Notification bell shows correct count
- [x] Badge updates in real-time (30s interval)
- [x] Bell animates on new notification
- [x] Employee can mark notification as read
- [x] Employee can mark all as read
- [x] Notification Details view works
- [x] Filtering (All/Unread) works
- [x] Empty states display correctly
- [x] Animations work smoothly
- [x] Responsive on mobile devices

---

## 🎉 Summary

The Contract Notification System provides a **complete, enterprise-grade notification solution** for keeping employees informed about their contract changes. With automatic notifications, real-time updates, beautiful UI, and comprehensive filtering, it ensures that no important contract update goes unnoticed.

**Key Benefits:**
- ✅ Transparency: Employees always know about contract changes
- ✅ Compliance: Documented notification trail
- ✅ User Experience: Beautiful, intuitive interface
- ✅ Real-time: Auto-updating badge and counts
- ✅ Professional: Enterprise-grade animations and design

---

**Last Updated:** December 16, 2025  
**Version:** 1.0  
**Status:** ✅ Production Ready




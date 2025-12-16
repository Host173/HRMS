# 🚀 Quick Fix Guide - Team Leave Approval Error

## ❌ Current Error
```
Microsoft.Data.SqlClient.SqlException: Invalid column name 'leave_type_id'.
Invalid column name 'is_active'.
```

---

## ✅ **THE FIX** (2 Minutes)

### Step 1: Run the Migration Script

1. **Open SQL Server Management Studio (SSMS)**
2. **Connect to your HRMS database**
3. **Open the file**: `HRMS/RUN_ALL_MIGRATIONS.sql`
4. **Click Execute** (or press F5)
5. **Wait for success message**:
   ```
   ✓ ALL MIGRATIONS COMPLETED SUCCESSFULLY!
   ```

### Step 2: Restart Your Application

1. **Stop the running application** (if it's running)
2. **Rebuild the solution** (optional but recommended)
3. **Start the application**
4. **Test the fix**:
   - Login as a Manager
   - Navigate to "Team Leave Approval" or "Leave Approval"
   - ✓ **Error should be gone!**

---

## 📋 What This Fixes

The migration script adds **missing database columns** needed by your HRMS application:

### Employee Table:
- ✅ `password_hash` - For login system
- ✅ Email index - For faster authentication

### LeaveRequest Table:
- ✅ `start_date` - Leave start date
- ✅ `end_date` - Leave end date
- ✅ `is_irregular` - Flag for irregular leaves
- ✅ `irregularity_reason` - Reason if flagged
- ✅ `created_at` - Request creation timestamp
- ✅ `justification` - Employee justification

### LeavePolicy Table: ⚠️ **THIS FIXES YOUR ERROR**
- ✅ `leave_type_id` - Links policy to leave type
- ✅ `is_active` - Whether policy is active
- ✅ `requires_hr_admin_approval` - HR approval flag
- ✅ `max_days_per_request` - Maximum days allowed
- ✅ `min_days_per_request` - Minimum days required
- ✅ `requires_documentation` - Documentation requirement
- ✅ `documentation_requirements` - Documentation details
- ✅ `approval_workflow` - Workflow configuration

---

## 🛡️ Safety Features

- ✅ **Safe to run multiple times** - Script checks for existing columns
- ✅ **Non-destructive** - Only adds columns, never deletes data
- ✅ **Backward compatible** - Existing data remains untouched
- ✅ **Transaction-safe** - Each migration is isolated

---

## 🧪 How to Test After Fix

### Test 1: Manager Leave Approval
1. Login with a **Manager** account
2. Go to **Leave Approval** menu
3. You should see leave requests (or "No pending requests")
4. ✅ **No SQL errors!**

### Test 2: Employee Login
1. Logout
2. Try logging in with an employee account
3. ✅ **Login should work** (password_hash column)

### Test 3: Leave Request Creation
1. Login as an employee
2. Create a new leave request
3. Fill in start date, end date, justification
4. ✅ **Request should save successfully**

---

## 🐛 Still Having Issues?

### Error: "Cannot open database 'HRMS'"
**Solution**: Update your connection string in `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=HRMS;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Error: "Scalar variable must be declared"
**Solution**: Restart your application after running migrations

### Error: "Object reference not set to an instance"
**Solution**: 
1. Make sure you're logged in as a Manager
2. Check that employees have `manager_id` set
3. Verify your Manager account has `is_active = 1`

---

## 📁 Files Reference

| File | Purpose |
|------|---------|
| `RUN_ALL_MIGRATIONS.sql` | **Master migration script (USE THIS!)** |
| `SQL_ADD_LEAVE_POLICY_COLUMNS.sql` | Individual: LeavePolicy columns |
| `SQL_ADD_LEAVE_REQUEST_COLUMNS.sql` | Individual: LeaveRequest columns |
| `SQL_ADD_IRREGULARITY_REASON.sql` | Individual: irregularity_reason |
| `SQL_ADD_PASSWORD_COLUMN.sql` | Individual: password_hash |
| `DATABASE_FIX_INSTRUCTIONS.md` | Detailed documentation |

**💡 TIP**: Use `RUN_ALL_MIGRATIONS.sql` - it runs everything in one go!

---

## ✅ Project Health Check

After running migrations, your project should have:
- ✅ **0 Compilation Errors**
- ✅ **0 Critical Warnings**
- ✅ **All Controllers Functional**
- ✅ **All Database Queries Working**

### Build Status
```
Build succeeded.
    0 Error(s)
    2 Warning(s) (NuGet package versions - safe to ignore)
```

---

## 🎯 Quick Command Reference

### Check if columns exist (SSMS):
```sql
-- Check LeavePolicy columns
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'LeavePolicy';

-- Check LeaveRequest columns
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'LeaveRequest';
```

### Verify migrations succeeded:
```sql
-- Should return 1 if columns exist
SELECT COUNT(*) FROM sys.columns 
WHERE object_id = OBJECT_ID('LeavePolicy') 
AND name IN ('leave_type_id', 'is_active');
```

---

## 📞 Support

If you're still experiencing issues after running the migration:

1. **Check the SQL Server error log**
2. **Verify database connection** in `appsettings.json`
3. **Ensure SQL Server is running**
4. **Check user permissions** (need ALTER TABLE rights)
5. **Review application logs** for detailed errors

---

## ⚡ Success Checklist

- [ ] Ran `RUN_ALL_MIGRATIONS.sql` in SSMS
- [ ] Saw "ALL MIGRATIONS COMPLETED SUCCESSFULLY"
- [ ] Restarted the application
- [ ] Can access Leave Approval page without errors
- [ ] Can login with employee accounts
- [ ] Can create/view leave requests

**All checked? You're good to go! 🎉**


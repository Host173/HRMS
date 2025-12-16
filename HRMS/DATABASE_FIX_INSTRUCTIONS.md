# 🔧 Database Fix Required - Leave Policy Columns Missing

## Problem
The Leave Approval feature is failing with SQL error:
```
Invalid column name 'leave_type_id'.
Invalid column name 'is_active'.
```

## Solution
You need to run the SQL migration script to add missing columns to the `LeavePolicy` table.

---

## 📋 Step-by-Step Instructions

### Option 1: Using SQL Server Management Studio (SSMS)

1. **Open SQL Server Management Studio**
2. **Connect to your HRMS database**
3. **Open the SQL script**:
   - Go to: `HRMS/SQL_ADD_LEAVE_POLICY_COLUMNS.sql`
   - Or copy the script below
4. **Execute the script** (Press F5 or click Execute)
5. **Verify success** - You should see messages like:
   ```
   Column leave_type_id added successfully.
   Column is_active added successfully.
   ...
   All columns have been added successfully!
   ```

### Option 2: Using Visual Studio

1. **Open SQL Server Object Explorer** (View → SQL Server Object Explorer)
2. **Right-click your HRMS database → New Query**
3. **Paste the contents** of `SQL_ADD_LEAVE_POLICY_COLUMNS.sql`
4. **Click Execute** (Green play button)

### Option 3: Using Command Line

```bash
sqlcmd -S your_server_name -d HRMS -i SQL_ADD_LEAVE_POLICY_COLUMNS.sql
```

Replace `your_server_name` with your SQL Server instance name.

---

## ✅ What This Script Does

The script adds the following columns to the `LeavePolicy` table:

| Column Name | Type | Description |
|-------------|------|-------------|
| `leave_type_id` | INT NULL | Links policy to leave type |
| `documentation_requirements` | NVARCHAR(MAX) NULL | Documentation rules |
| `approval_workflow` | NVARCHAR(500) NULL | Approval process details |
| `is_active` | BIT NOT NULL (Default: 1) | Whether policy is active |
| `requires_hr_admin_approval` | BIT NOT NULL (Default: 0) | Needs HR approval |
| `max_days_per_request` | INT NULL | Maximum days per request |
| `min_days_per_request` | INT NULL | Minimum days per request |
| `requires_documentation` | BIT NOT NULL (Default: 0) | Documentation required |

---

## 🛡️ Safety Features

- ✅ **Safe to run multiple times** - Uses `IF NOT EXISTS` checks
- ✅ **Non-destructive** - Only adds columns, doesn't modify existing data
- ✅ **Backward compatible** - All columns are nullable or have defaults

---

## 🧪 Verify the Fix

After running the script:

1. **Restart your application**
2. **Login as a Manager**
3. **Navigate to**: Leave Approval (or Team Leave Approval)
4. **You should see** the leave requests without errors

---

## 📞 Still Having Issues?

If the error persists:

1. **Check the database name**: Make sure you're connected to the `HRMS` database
2. **Check permissions**: Ensure your SQL user has ALTER TABLE permissions
3. **Check SQL Server version**: Script is compatible with SQL Server 2012+
4. **View error messages**: Look for specific errors in the SSMS messages tab

---

## 🔗 Related Files

- Migration Script: `HRMS/SQL_ADD_LEAVE_POLICY_COLUMNS.sql`
- Controller: `HRMS/Controllers/LeaveApprovalController.cs` (lines 51-52, 154, 209-211)
- Model: `HRMS/Models/LeavePolicy.cs` (lines 23-37)
- DbContext: `HRMS/Data/HrmsDbContext.cs` (lines 628-634)




# Fix for Leave Policy Database Error

## Problem

When accessing Team Leave Approval (`LeaveApprovalController/Index`), you encounter:

```
Microsoft.Data.SqlClient.SqlException: 'Invalid column name 'leave_type_id'.
Invalid column name 'is_active'.
```

## Root Cause

The `LeavePolicy` table in your database is missing required columns that were added to the C# model but never added to the actual database table.

## Solution

Run the SQL script to add the missing columns to your database.

### Step 1: Run the SQL Script

1. Open **SQL Server Management Studio** (SSMS)
2. Connect to your HRMS database
3. Open the file: `HRMS/SQL_ADD_LEAVE_POLICY_COLUMNS.sql`
4. Execute the script
5. Verify the output shows all columns were added successfully

### Step 2: Restart the Application

After running the SQL script, restart your ASP.NET application for the changes to take effect.

### Step 3: Test the Fix

1. Log in as a Line Manager
2. Navigate to "Team Leave Approval" or `LeaveApproval/Index`
3. The page should now load without errors

## What the SQL Script Does

The script adds the following columns to the `LeavePolicy` table:

- `leave_type_id` (INT, nullable) - Links policy to specific leave type
- `documentation_requirements` (NVARCHAR(MAX), nullable) - Documentation needs
- `approval_workflow` (NVARCHAR(500), nullable) - Approval process description
- `is_active` (BIT, default 1) - Whether policy is active
- `requires_hr_admin_approval` (BIT, default 0) - If HR approval is needed
- `max_days_per_request` (INT, nullable) - Maximum days per request
- `min_days_per_request` (INT, nullable) - Minimum days per request
- `requires_documentation` (BIT, default 0) - If documentation is required

The script is safe to run multiple times - it checks if columns exist before adding them.

## Alternative: Quick Fix (Temporary)

If you can't run the SQL script immediately, you can temporarily comment out the problematic code in `LeaveApprovalController.cs`, but this is NOT recommended for production use.

## Files Affected

The following files use these LeavePolicy columns:
- `Controllers/LeaveApprovalController.cs` (lines 52, 154, 210)
- `Controllers/SpecialLeaveController.cs` (lines 35, 92, 164, 210, 256, 315)

All of these will work correctly once the SQL script is executed.


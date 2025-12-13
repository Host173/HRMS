# HR Admin Leave Management System - Setup Guide

## ✅ What Has Been Implemented

### 1. Leave Type Management
- **Controller**: `LeaveTypeController`
- **Views**: Index, Create, Edit
- **Features**:
  - View all leave types
  - Add new leave types (Vacation, Sick, Probation, Holiday, Special, etc.)
  - Edit existing leave types
  - Delete leave types (if no requests exist)
  - View request count per leave type

### 2. Leave Policy Management
- **Controller**: `LeavePolicyController`
- **Views**: Index, Create, Edit
- **Features**:
  - Create/Edit/Activate/Deactivate policies per leave type
  - Configure eligibility rules
  - Set documentation requirements
  - Define approval workflow
  - Set minimum/maximum days per request
  - Configure notice period
  - Mark policies as requiring HR Admin approval only
  - Filter policies by leave type

### 3. Leave Entitlement Management
- **Controller**: `LeaveEntitlementController`
- **Views**: Index, Adjust
- **Features**:
  - View all employee leave entitlements
  - Adjust entitlements per employee and leave type
  - Filter by employee or leave type
  - Set custom entitlement values

### 4. Leave Override Functionality
- **Controller**: `LeaveOverrideController`
- **Views**: Index, Review
- **Features**:
  - View all leave requests (including those approved/rejected by line managers)
  - Override line manager approvals
  - Approve or reject any leave request
  - Flag requests as irregular

### 5. Policy Enforcement
- **Updated**: `LeaveRequestController`
- **Features**:
  - Enforces minimum/maximum days per request
  - Validates notice period requirements
  - Checks documentation requirements
  - Only shows leave types created by HR Admin
  - Validates eligibility rules

### 6. Special Leave Type Handling
- **Updated**: `LeaveApprovalController`
- **Features**:
  - Line managers cannot approve special leave types
  - Special leave types require HR Admin approval only
  - Automatic policy checking before approval

### 7. HR Admin Dashboard
- **Updated**: `Home/Index.cshtml`
- **Features**:
  - Leave Management section with links to:
    - Manage Leave Types
    - Manage Leave Policies
    - Adjust Leave Entitlements
    - Override Leave Approvals

## 📋 Required Database Setup

### Step 1: Run Leave Policy Migration

Run the SQL script to add new columns to the `LeavePolicy` table:

**File**: `HRMS/SQL_ADD_LEAVE_POLICY_COLUMNS.sql`

**Columns Added**:
- `leave_type_id` (INT, nullable) - Links policy to leave type
- `documentation_requirements` (NVARCHAR(MAX), nullable)
- `approval_workflow` (NVARCHAR(500), nullable)
- `is_active` (BIT, default 1)
- `requires_hr_admin_approval` (BIT, default 0)
- `max_days_per_request` (INT, nullable)
- `min_days_per_request` (INT, nullable)
- `requires_documentation` (BIT, default 0)

**How to Run**:
1. Open SQL Server Management Studio
2. Connect to: `HESTIA\SQLEXPRESS`
3. Select database: `HRMS`
4. Open file: `HRMS/SQL_ADD_LEAVE_POLICY_COLUMNS.sql`
5. Execute (F5)

Or use PowerShell:
```powershell
sqlcmd -S "HESTIA\SQLEXPRESS" -d "HRMS" -i "HRMS\SQL_ADD_LEAVE_POLICY_COLUMNS.sql" -E
```

## 🎯 Usage Guide

### For HR Admins:

1. **Create Leave Types**:
   - Go to Dashboard → Leave Management → Manage Leave Types
   - Click "Add New Leave Type"
   - Enter type name (e.g., "Vacation Leave", "Sick Leave", "Special Leave")
   - Add description
   - Save

2. **Create Leave Policies**:
   - Go to Dashboard → Leave Management → Manage Leave Policies
   - Click "Create New Policy"
   - Select leave type
   - Configure:
     - Eligibility rules
     - Documentation requirements
     - Approval workflow
     - Notice period
     - Min/Max days
     - Check "Requires HR Admin Approval Only" for special leave types
   - Save

3. **Adjust Leave Entitlements**:
   - Go to Dashboard → Leave Management → Adjust Leave Entitlements
   - Click "Adjust Entitlement"
   - Select employee and leave type
   - Set entitlement days
   - Save

4. **Override Leave Approvals**:
   - Go to Dashboard → Leave Management → Override Leave Approvals
   - View all leave requests
   - Click "Review" on any request
   - Override approve/reject as needed

### Policy Enforcement:

When employees create leave requests:
- System checks if leave type exists (created by HR Admin)
- Validates minimum/maximum days based on policy
- Checks notice period requirements
- Validates documentation requirements
- Enforces eligibility rules

### Special Leave Types:

- Mark a leave type as "Special Leave" in the policy
- Check "Requires HR Admin Approval Only"
- Line managers will see these requests but cannot approve them
- Only HR Admins can approve special leave types

## 🔐 Access Control

- **Leave Type Management**: HR Admin only
- **Leave Policy Management**: HR Admin only
- **Leave Entitlement Adjustment**: HR Admin only
- **Leave Override**: HR Admin only
- **Leave Request Creation**: All employees (but only HR Admin created types are shown)
- **Leave Approval**: Line Managers (except for special leave types)

## 📝 Notes

- Leave types must be created by HR Admin before employees can use them
- Policies are enforced during leave request submission
- Special leave types automatically require HR Admin approval
- HR Admins can override any line manager decision
- Leave entitlements can be adjusted per employee and leave type






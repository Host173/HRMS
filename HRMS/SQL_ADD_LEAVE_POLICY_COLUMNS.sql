-- SQL Script to Add New Columns to LeavePolicy Table
-- Run this script in SQL Server Management Studio or your SQL tool
-- Make sure you're connected to the HRMS database

USE HRMS;
GO

-- Add leave_type_id column (INT, nullable, foreign key to Leave table)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'leave_type_id'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD leave_type_id INT NULL;
    
    PRINT 'Column leave_type_id added successfully.';
END
ELSE
BEGIN
    PRINT 'Column leave_type_id already exists.';
END
GO

-- Add documentation_requirements column (NVARCHAR(MAX), nullable)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'documentation_requirements'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD documentation_requirements NVARCHAR(MAX) NULL;
    
    PRINT 'Column documentation_requirements added successfully.';
END
ELSE
BEGIN
    PRINT 'Column documentation_requirements already exists.';
END
GO

-- Add approval_workflow column (NVARCHAR(500), nullable)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'approval_workflow'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD approval_workflow NVARCHAR(500) NULL;
    
    PRINT 'Column approval_workflow added successfully.';
END
ELSE
BEGIN
    PRINT 'Column approval_workflow already exists.';
END
GO

-- Add is_active column (BIT, default 1)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'is_active'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD is_active BIT NOT NULL DEFAULT 1;
    
    PRINT 'Column is_active added successfully.';
END
ELSE
BEGIN
    PRINT 'Column is_active already exists.';
END
GO

-- Add requires_hr_admin_approval column (BIT, default 0)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'requires_hr_admin_approval'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD requires_hr_admin_approval BIT NOT NULL DEFAULT 0;
    
    PRINT 'Column requires_hr_admin_approval added successfully.';
END
ELSE
BEGIN
    PRINT 'Column requires_hr_admin_approval already exists.';
END
GO

-- Add max_days_per_request column (INT, nullable)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'max_days_per_request'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD max_days_per_request INT NULL;
    
    PRINT 'Column max_days_per_request added successfully.';
END
ELSE
BEGIN
    PRINT 'Column max_days_per_request already exists.';
END
GO

-- Add min_days_per_request column (INT, nullable)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'min_days_per_request'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD min_days_per_request INT NULL;
    
    PRINT 'Column min_days_per_request added successfully.';
END
ELSE
BEGIN
    PRINT 'Column min_days_per_request already exists.';
END
GO

-- Add requires_documentation column (BIT, default 0)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'requires_documentation'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD requires_documentation BIT NOT NULL DEFAULT 0;
    
    PRINT 'Column requires_documentation added successfully.';
END
ELSE
BEGIN
    PRINT 'Column requires_documentation already exists.';
END
GO

-- Optional: Add foreign key constraint for leave_type_id
-- Uncomment if you want to enforce referential integrity
/*
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_LeavePolicy_Leave'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD CONSTRAINT FK_LeavePolicy_Leave
    FOREIGN KEY (leave_type_id) REFERENCES Leave(leave_id);
    
    PRINT 'Foreign key constraint FK_LeavePolicy_Leave added successfully.';
END
ELSE
BEGIN
    PRINT 'Foreign key constraint FK_LeavePolicy_Leave already exists.';
END
GO
*/

PRINT 'All columns have been added successfully!';
GO






-- ============================================================================
-- HRMS Database - Complete Migration Script
-- ============================================================================
-- This script adds all missing columns required for the HRMS application
-- It is SAFE to run multiple times - it checks for existing columns first
-- ============================================================================

USE HRMS;
GO

PRINT '============================================================================';
PRINT 'Starting HRMS Database Migrations...';
PRINT '============================================================================';
PRINT '';

-- ============================================================================
-- MIGRATION 1: Add password_hash to Employee table
-- ============================================================================
PRINT '--- MIGRATION 1: Employee Table ---';

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('Employee') 
    AND name = 'password_hash'
)
BEGIN
    ALTER TABLE Employee
    ADD password_hash VARCHAR(255) NULL;
    
    PRINT '✓ Column password_hash added to Employee table';
END
ELSE
BEGIN
    PRINT '✓ Column password_hash already exists in Employee table';
END
GO

-- Create index on email for faster login lookups
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE object_id = OBJECT_ID('Employee') 
    AND name = 'IX_Employee_Email'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Employee_Email
    ON Employee(email)
    WHERE email IS NOT NULL;
    
    PRINT '✓ Index IX_Employee_Email created on Employee table';
END
ELSE
BEGIN
    PRINT '✓ Index IX_Employee_Email already exists';
END
GO

PRINT '';

-- ============================================================================
-- MIGRATION 2: Add columns to LeaveRequest table
-- ============================================================================
PRINT '--- MIGRATION 2: LeaveRequest Table ---';

-- Add start_date
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'start_date'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD start_date DATE NULL;
    
    PRINT '✓ Column start_date added to LeaveRequest table';
END
ELSE
BEGIN
    PRINT '✓ Column start_date already exists in LeaveRequest table';
END
GO

-- Add end_date
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'end_date'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD end_date DATE NULL;
    
    PRINT '✓ Column end_date added to LeaveRequest table';
END
ELSE
BEGIN
    PRINT '✓ Column end_date already exists in LeaveRequest table';
END
GO

-- Add is_irregular
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'is_irregular'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD is_irregular BIT NOT NULL DEFAULT 0;
    
    PRINT '✓ Column is_irregular added to LeaveRequest table';
END
ELSE
BEGIN
    PRINT '✓ Column is_irregular already exists in LeaveRequest table';
END
GO

-- Add created_at
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'created_at'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD created_at DATETIME NULL;
    
    PRINT '✓ Column created_at added to LeaveRequest table';
END
ELSE
BEGIN
    PRINT '✓ Column created_at already exists in LeaveRequest table';
END
GO

-- Add irregularity_reason
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'irregularity_reason'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD irregularity_reason NVARCHAR(MAX) NULL;
    
    PRINT '✓ Column irregularity_reason added to LeaveRequest table';
END
ELSE
BEGIN
    PRINT '✓ Column irregularity_reason already exists in LeaveRequest table';
END
GO

-- Add justification (if not exists)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'justification'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD justification NVARCHAR(MAX) NULL;
    
    PRINT '✓ Column justification added to LeaveRequest table';
END
ELSE
BEGIN
    PRINT '✓ Column justification already exists in LeaveRequest table';
END
GO

PRINT '';

-- ============================================================================
-- MIGRATION 3: Add columns to LeavePolicy table
-- ============================================================================
PRINT '--- MIGRATION 3: LeavePolicy Table ---';

-- Add leave_type_id
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'leave_type_id'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD leave_type_id INT NULL;
    
    PRINT '✓ Column leave_type_id added to LeavePolicy table';
END
ELSE
BEGIN
    PRINT '✓ Column leave_type_id already exists in LeavePolicy table';
END
GO

-- Add documentation_requirements
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'documentation_requirements'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD documentation_requirements NVARCHAR(MAX) NULL;
    
    PRINT '✓ Column documentation_requirements added to LeavePolicy table';
END
ELSE
BEGIN
    PRINT '✓ Column documentation_requirements already exists in LeavePolicy table';
END
GO

-- Add approval_workflow
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'approval_workflow'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD approval_workflow NVARCHAR(500) NULL;
    
    PRINT '✓ Column approval_workflow added to LeavePolicy table';
END
ELSE
BEGIN
    PRINT '✓ Column approval_workflow already exists in LeavePolicy table';
END
GO

-- Add is_active
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'is_active'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD is_active BIT NOT NULL DEFAULT 1;
    
    PRINT '✓ Column is_active added to LeavePolicy table';
END
ELSE
BEGIN
    PRINT '✓ Column is_active already exists in LeavePolicy table';
END
GO

-- Add requires_hr_admin_approval
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'requires_hr_admin_approval'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD requires_hr_admin_approval BIT NOT NULL DEFAULT 0;
    
    PRINT '✓ Column requires_hr_admin_approval added to LeavePolicy table';
END
ELSE
BEGIN
    PRINT '✓ Column requires_hr_admin_approval already exists in LeavePolicy table';
END
GO

-- Add max_days_per_request
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'max_days_per_request'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD max_days_per_request INT NULL;
    
    PRINT '✓ Column max_days_per_request added to LeavePolicy table';
END
ELSE
BEGIN
    PRINT '✓ Column max_days_per_request already exists in LeavePolicy table';
END
GO

-- Add min_days_per_request
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'min_days_per_request'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD min_days_per_request INT NULL;
    
    PRINT '✓ Column min_days_per_request added to LeavePolicy table';
END
ELSE
BEGIN
    PRINT '✓ Column min_days_per_request already exists in LeavePolicy table';
END
GO

-- Add requires_documentation
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') 
    AND name = 'requires_documentation'
)
BEGIN
    ALTER TABLE LeavePolicy
    ADD requires_documentation BIT NOT NULL DEFAULT 0;
    
    PRINT '✓ Column requires_documentation added to LeavePolicy table';
END
ELSE
BEGIN
    PRINT '✓ Column requires_documentation already exists in LeavePolicy table';
END
GO

PRINT '';
PRINT '============================================================================';
PRINT '✓ ALL MIGRATIONS COMPLETED SUCCESSFULLY!';
PRINT '============================================================================';
PRINT '';
PRINT 'Summary:';
PRINT '- Employee table: password_hash column + email index';
PRINT '- LeaveRequest table: start_date, end_date, is_irregular, created_at, irregularity_reason, justification';
PRINT '- LeavePolicy table: leave_type_id, is_active, requires_hr_admin_approval, and 5 other columns';
PRINT '';
PRINT 'Your HRMS database is now up to date!';
PRINT 'You can restart your application and all features should work correctly.';
PRINT '============================================================================';
GO




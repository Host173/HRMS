-- ============================================================================
-- HRMS DATABASE MIGRATIONS - RUN ALL
-- ============================================================================
-- This script runs all database migrations required for the HRMS application
-- It is SAFE to run multiple times - it checks for existing columns first
-- ============================================================================

USE HRMS;
GO

PRINT '========================================';
PRINT 'Starting HRMS Database Migrations';
PRINT '========================================';
PRINT '';

-- ============================================================================
-- MIGRATION 1: Add password_hash column to Employee table
-- ============================================================================
PRINT 'Migration 1: Employee password_hash column';
PRINT '----------------------------------------';

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('Employee') 
    AND name = 'password_hash'
)
BEGIN
    ALTER TABLE Employee
    ADD password_hash VARCHAR(255) NULL;
    
    PRINT '✓ password_hash column added successfully';
END
ELSE
BEGIN
    PRINT '✓ password_hash column already exists';
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
    
    PRINT '✓ Index IX_Employee_Email created successfully';
END
ELSE
BEGIN
    PRINT '✓ Index IX_Employee_Email already exists';
END
GO

PRINT '';

-- ============================================================================
-- MIGRATION 2: Add new columns to LeaveRequest table
-- ============================================================================
PRINT 'Migration 2: LeaveRequest table columns';
PRINT '----------------------------------------';

-- Add start_date
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') AND name = 'start_date'
)
BEGIN
    ALTER TABLE LeaveRequest ADD start_date DATE NULL;
    PRINT '✓ start_date column added';
END
ELSE
BEGIN
    PRINT '✓ start_date already exists';
END
GO

-- Add end_date
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') AND name = 'end_date'
)
BEGIN
    ALTER TABLE LeaveRequest ADD end_date DATE NULL;
    PRINT '✓ end_date column added';
END
ELSE
BEGIN
    PRINT '✓ end_date already exists';
END
GO

-- Add is_irregular
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') AND name = 'is_irregular'
)
BEGIN
    ALTER TABLE LeaveRequest ADD is_irregular BIT NOT NULL DEFAULT 0;
    PRINT '✓ is_irregular column added';
END
ELSE
BEGIN
    PRINT '✓ is_irregular already exists';
END
GO

-- Add created_at
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') AND name = 'created_at'
)
BEGIN
    ALTER TABLE LeaveRequest ADD created_at DATETIME NULL;
    PRINT '✓ created_at column added';
END
ELSE
BEGIN
    PRINT '✓ created_at already exists';
END
GO

-- Add irregularity_reason
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') AND name = 'irregularity_reason'
)
BEGIN
    ALTER TABLE LeaveRequest ADD irregularity_reason NVARCHAR(MAX) NULL;
    PRINT '✓ irregularity_reason column added';
END
ELSE
BEGIN
    PRINT '✓ irregularity_reason already exists';
END
GO

-- Add justification
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') AND name = 'justification'
)
BEGIN
    ALTER TABLE LeaveRequest ADD justification NVARCHAR(MAX) NULL;
    PRINT '✓ justification column added';
END
ELSE
BEGIN
    PRINT '✓ justification already exists';
END
GO

PRINT '';

-- ============================================================================
-- MIGRATION 3: Add new columns to LeavePolicy table
-- ============================================================================
PRINT 'Migration 3: LeavePolicy table columns';
PRINT '----------------------------------------';

-- Add leave_type_id
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') AND name = 'leave_type_id'
)
BEGIN
    ALTER TABLE LeavePolicy ADD leave_type_id INT NULL;
    PRINT '✓ leave_type_id column added';
END
ELSE
BEGIN
    PRINT '✓ leave_type_id already exists';
END
GO

-- Add documentation_requirements
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') AND name = 'documentation_requirements'
)
BEGIN
    ALTER TABLE LeavePolicy ADD documentation_requirements NVARCHAR(MAX) NULL;
    PRINT '✓ documentation_requirements column added';
END
ELSE
BEGIN
    PRINT '✓ documentation_requirements already exists';
END
GO

-- Add approval_workflow
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') AND name = 'approval_workflow'
)
BEGIN
    ALTER TABLE LeavePolicy ADD approval_workflow NVARCHAR(500) NULL;
    PRINT '✓ approval_workflow column added';
END
ELSE
BEGIN
    PRINT '✓ approval_workflow already exists';
END
GO

-- Add is_active
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') AND name = 'is_active'
)
BEGIN
    ALTER TABLE LeavePolicy ADD is_active BIT NOT NULL DEFAULT 1;
    PRINT '✓ is_active column added';
END
ELSE
BEGIN
    PRINT '✓ is_active already exists';
END
GO

-- Add requires_hr_admin_approval
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') AND name = 'requires_hr_admin_approval'
)
BEGIN
    ALTER TABLE LeavePolicy ADD requires_hr_admin_approval BIT NOT NULL DEFAULT 0;
    PRINT '✓ requires_hr_admin_approval column added';
END
ELSE
BEGIN
    PRINT '✓ requires_hr_admin_approval already exists';
END
GO

-- Add max_days_per_request
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') AND name = 'max_days_per_request'
)
BEGIN
    ALTER TABLE LeavePolicy ADD max_days_per_request INT NULL;
    PRINT '✓ max_days_per_request column added';
END
ELSE
BEGIN
    PRINT '✓ max_days_per_request already exists';
END
GO

-- Add min_days_per_request
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') AND name = 'min_days_per_request'
)
BEGIN
    ALTER TABLE LeavePolicy ADD min_days_per_request INT NULL;
    PRINT '✓ min_days_per_request column added';
END
ELSE
BEGIN
    PRINT '✓ min_days_per_request already exists';
END
GO

-- Add requires_documentation
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeavePolicy') AND name = 'requires_documentation'
)
BEGIN
    ALTER TABLE LeavePolicy ADD requires_documentation BIT NOT NULL DEFAULT 0;
    PRINT '✓ requires_documentation column added';
END
ELSE
BEGIN
    PRINT '✓ requires_documentation already exists';
END
GO

PRINT '';
PRINT '========================================';
PRINT 'All migrations completed successfully!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Restart your ASP.NET application';
PRINT '2. Test Team Leave Approval functionality';
PRINT '3. Test Special Leave functionality';
PRINT '4. Test Login/Registration';
PRINT '';
GO


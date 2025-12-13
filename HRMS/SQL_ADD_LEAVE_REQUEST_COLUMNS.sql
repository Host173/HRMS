-- SQL Script to Add New Columns to LeaveRequest Table
-- Run this script in SQL Server Management Studio or your SQL tool
-- Make sure you're connected to the HRMS database

USE HRMS;
GO

-- Add start_date column (DATE, nullable)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'start_date'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD start_date DATE NULL;
    
    PRINT 'Column start_date added successfully.';
END
ELSE
BEGIN
    PRINT 'Column start_date already exists.';
END
GO

-- Add end_date column (DATE, nullable)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'end_date'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD end_date DATE NULL;
    
    PRINT 'Column end_date added successfully.';
END
ELSE
BEGIN
    PRINT 'Column end_date already exists.';
END
GO

-- Add is_irregular column (BIT, default 0)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'is_irregular'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD is_irregular BIT NOT NULL DEFAULT 0;
    
    PRINT 'Column is_irregular added successfully.';
END
ELSE
BEGIN
    PRINT 'Column is_irregular already exists.';
END
GO

-- Add created_at column (DATETIME, nullable)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('LeaveRequest') 
    AND name = 'created_at'
)
BEGIN
    ALTER TABLE LeaveRequest
    ADD created_at DATETIME NULL;
    
    PRINT 'Column created_at added successfully.';
END
ELSE
BEGIN
    PRINT 'Column created_at already exists.';
END
GO

-- Optional: Update existing records to set created_at to current time if null
-- Uncomment the following if you want to set a default created_at for existing records
/*
UPDATE LeaveRequest
SET created_at = GETUTCDATE()
WHERE created_at IS NULL;
GO
*/

PRINT 'All columns have been added successfully!';
GO


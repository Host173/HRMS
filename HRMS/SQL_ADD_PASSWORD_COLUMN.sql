-- SQL Script to add password_hash column to Employee table
-- Run this script on your HRMS database before using the login system

-- Check if column already exists, if not add it
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('Employee') 
    AND name = 'password_hash'
)
BEGIN
    ALTER TABLE Employee
    ADD password_hash VARCHAR(255) NULL;
    
    PRINT 'password_hash column added successfully to Employee table';
END
ELSE
BEGIN
    PRINT 'password_hash column already exists in Employee table';
END
GO

-- Optional: Create index on email for faster login lookups (if not already exists)
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
    
    PRINT 'Index IX_Employee_Email created successfully';
END
ELSE
BEGIN
    PRINT 'Index IX_Employee_Email already exists';
END
GO


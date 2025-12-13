-- Add irregularity_reason column to LeaveRequest table
-- This script is idempotent and can be run multiple times safely

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequest]') 
    AND name = 'irregularity_reason'
)
BEGIN
    ALTER TABLE [dbo].[LeaveRequest]
    ADD irregularity_reason NVARCHAR(MAX) NULL;
    
    PRINT 'Column irregularity_reason added to LeaveRequest table.';
END
ELSE
BEGIN
    PRINT 'Column irregularity_reason already exists in LeaveRequest table.';
END
GO



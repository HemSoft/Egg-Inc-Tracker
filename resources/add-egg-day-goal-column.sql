-- Add EggDayGoal column to Goals table
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Goals]') 
    AND name = 'EggDayGoal'
)
BEGIN
    ALTER TABLE [dbo].[Goals]
    ADD [EggDayGoal] nvarchar(max) NULL;
    
    PRINT 'EggDayGoal column added to Goals table.';
END
ELSE
BEGIN
    PRINT 'EggDayGoal column already exists in Goals table.';
END

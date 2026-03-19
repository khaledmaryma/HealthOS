-- Setup Script for ResultType Table and Foreign Key Relationship
-- Database: LIS
-- Purpose: Ensure ResultType table exists with proper data and relationship to LabTest

USE [LIS]
GO

-- =============================================
-- Step 1: Check if ResultType table exists
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ResultType' AND type = 'U')
BEGIN
    PRINT 'Creating ResultType table...'
    
    CREATE TABLE [dbo].[ResultType] (
        [ID] INT NOT NULL PRIMARY KEY,
        [Description] NVARCHAR(50) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    
    PRINT 'ResultType table created successfully.'
END
ELSE
BEGIN
    PRINT 'ResultType table already exists.'
END
GO

-- =============================================
-- Step 2: Ensure required columns exist
-- =============================================
-- Check for Description column
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[ResultType]') 
               AND name = 'Description')
BEGIN
    PRINT 'Adding Description column...'
    ALTER TABLE [dbo].[ResultType] ADD [Description] NVARCHAR(50) NULL;
END

-- Check for IsActive column
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[ResultType]') 
               AND name = 'IsActive')
BEGIN
    PRINT 'Adding IsActive column...'
    ALTER TABLE [dbo].[ResultType] ADD [IsActive] BIT NOT NULL DEFAULT 1;
END
GO

-- =============================================
-- Step 3: Insert default result types if empty
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[ResultType])
BEGIN
    PRINT 'Inserting default result types...'
    
    INSERT INTO [dbo].[ResultType] ([ID], [Description], [IsActive])
    VALUES 
        (1, 'Numeric', 1),
        (2, 'Text', 1);
    
    PRINT 'Default result types inserted.'
END
ELSE
BEGIN
    PRINT 'ResultType table already contains data.'
    
    -- Display existing data
    SELECT * FROM [dbo].[ResultType];
END
GO

-- =============================================
-- Step 4: Check for orphaned LabTest records
-- =============================================
PRINT 'Checking for LabTest records with invalid ResultType...'

IF EXISTS (
    SELECT lt.ID, lt.Code, lt.ResultType 
    FROM [dbo].[LabTest] lt
    WHERE lt.ResultType IS NOT NULL 
      AND lt.ResultType NOT IN (SELECT ID FROM [dbo].[ResultType])
)
BEGIN
    PRINT 'WARNING: Found LabTest records with invalid ResultType values:'
    
    SELECT lt.ID, lt.Code, lt.TestDesciption, lt.ResultType 
    FROM [dbo].[LabTest] lt
    WHERE lt.ResultType IS NOT NULL 
      AND lt.ResultType NOT IN (SELECT ID FROM [dbo].[ResultType]);
    
    PRINT ''
    PRINT 'You may want to update these records before adding the foreign key constraint.'
    PRINT 'Example: UPDATE LabTest SET ResultType = 2 WHERE ResultType NOT IN (SELECT ID FROM ResultType);'
END
ELSE
BEGIN
    PRINT 'All LabTest records have valid ResultType values.'
END
GO

-- =============================================
-- Step 5: Add Foreign Key Constraint
-- =============================================
-- First, check if the constraint already exists
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE name = 'FK_LabTest_ResultType_ResultType'
    AND parent_object_id = OBJECT_ID(N'[dbo].[LabTest]')
)
BEGIN
    PRINT 'Adding foreign key constraint FK_LabTest_ResultType_ResultType...'
    
    -- Create index on foreign key column for better performance
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LabTest_ResultType' AND object_id = OBJECT_ID(N'[dbo].[LabTest]'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_LabTest_ResultType] 
        ON [dbo].[LabTest] ([ResultType]);
        PRINT 'Index IX_LabTest_ResultType created.'
    END
    
    -- Add foreign key constraint with RESTRICT (NO ACTION) delete behavior
    ALTER TABLE [dbo].[LabTest]
    ADD CONSTRAINT [FK_LabTest_ResultType_ResultType] 
    FOREIGN KEY ([ResultType]) 
    REFERENCES [dbo].[ResultType] ([ID])
    ON DELETE NO ACTION
    ON UPDATE NO ACTION;
    
    PRINT 'Foreign key constraint added successfully.'
END
ELSE
BEGIN
    PRINT 'Foreign key constraint FK_LabTest_ResultType_ResultType already exists.'
END
GO

-- =============================================
-- Step 6: Verify the setup
-- =============================================
PRINT ''
PRINT '===== VERIFICATION ====='
PRINT ''

-- Show ResultType data
PRINT 'ResultType data:'
SELECT * FROM [dbo].[ResultType] WHERE IsActive = 1;
PRINT ''

-- Show count of LabTests by ResultType
PRINT 'LabTest count by ResultType:'
SELECT 
    rt.ID,
    rt.Description,
    COUNT(lt.ID) AS LabTestCount
FROM [dbo].[ResultType] rt
LEFT JOIN [dbo].[LabTest] lt ON lt.ResultType = rt.ID AND lt.IsDeleted = 0
GROUP BY rt.ID, rt.Description
ORDER BY rt.ID;
PRINT ''

-- Show foreign key constraint details
PRINT 'Foreign key constraint details:'
SELECT 
    fk.name AS ForeignKey,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
    OBJECT_NAME (fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fc 
    ON fk.object_id = fc.constraint_object_id
WHERE fk.name = 'FK_LabTest_ResultType_ResultType';

PRINT ''
PRINT '===== SETUP COMPLETE ====='
GO


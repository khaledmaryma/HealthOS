-- Check actual column names in HospitalDefinition.dbo.Denomination
USE HospitalDefinition;
GO

-- Show all columns
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Denomination'
ORDER BY ORDINAL_POSITION;

-- Show sample data
SELECT TOP 5 * 
FROM Denomination 
WHERE IsDeleted = 0 AND CostCenter = 1;























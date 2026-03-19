-- Check Denomination table structure
USE HospitalDefinition;
GO

-- Table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Denomination'
ORDER BY ORDINAL_POSITION;

-- Sample data (filtered by CostCenter = 1)
SELECT TOP 10 * 
FROM Denomination 
WHERE IsDeleted = 0 AND CostCenter = 1 
ORDER BY DisplayOrder, Description;

-- Row count
SELECT COUNT(*) AS Total_Rows 
FROM Denomination 
WHERE IsDeleted = 0 AND CostCenter = 1;


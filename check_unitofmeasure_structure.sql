-- Query to check the actual structure of UnitOfMeasure table in EMR database
USE [EMR]
GO

-- Get column information
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS Precision,
    c.scale AS Scale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity,
    CASE WHEN pk.column_id IS NOT NULL THEN 'YES' ELSE 'NO' END AS IsPrimaryKey
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
LEFT JOIN (
    SELECT ic.object_id, ic.column_id
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    WHERE i.is_primary_key = 1
) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
WHERE c.object_id = OBJECT_ID('UnitOfMeasure')
ORDER BY c.column_id;

-- Get sample data
SELECT TOP 10 * FROM UnitOfMeasure;

-- Test cross-database query from LIS database
USE [LIS]
GO

SELECT TOP 5 * FROM EMR.dbo.UnitOfMeasure;


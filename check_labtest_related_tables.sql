-- Query to check the actual structure of LabTest related tables
USE [LIS]
GO

-- LabTestAge table structure
PRINT '===== LabTestAge Table ====='
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('LabTestAge')
ORDER BY c.column_id;

SELECT TOP 5 * FROM LabTestAge;
PRINT ''

-- LabTestGyneco table structure
PRINT '===== LabTestGyneco Table ====='
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('LabTestGyneco')
ORDER BY c.column_id;

SELECT TOP 5 * FROM LabTestGyneco;
PRINT ''

-- LabTestSub table structure
PRINT '===== LabTestSub Table ====='
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('LabTestSub')
ORDER BY c.column_id;

SELECT TOP 5 * FROM LabTestSub;


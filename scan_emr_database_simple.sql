-- Simple EMR Database Scan
-- This script provides a basic overview of the EMR database structure

USE [EMR];
GO

-- 1. List all tables
SELECT 
    'TABLE' AS ObjectType,
    t.name AS ObjectName,
    s.name AS SchemaName,
    (SELECT SUM(p.rows) 
     FROM sys.partitions p 
     WHERE p.object_id = t.object_id AND p.index_id IN (0,1)) AS RowCount
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'dbo'
ORDER BY t.name;

-- 2. List all views
SELECT 
    'VIEW' AS ObjectType,
    v.name AS ObjectName,
    s.name AS SchemaName,
    NULL AS RowCount
FROM sys.views v
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
WHERE s.name = 'dbo'
ORDER BY v.name;

-- 3. List all stored procedures
SELECT 
    'STORED PROCEDURE' AS ObjectType,
    p.name AS ObjectName,
    s.name AS SchemaName,
    NULL AS RowCount
FROM sys.procedures p
INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
WHERE s.name = 'dbo'
ORDER BY p.name;

-- 4. UnitOfMeasure table structure (known to be used)
IF OBJECT_ID('UnitOfMeasure', 'U') IS NOT NULL
BEGIN
    SELECT 
        'UnitOfMeasure Columns' AS Info,
        c.name AS ColumnName,
        t.name AS DataType,
        CAST(c.max_length AS VARCHAR(10)) AS MaxLength,
        CAST(c.precision AS VARCHAR(10)) AS Precision,
        CAST(c.scale AS VARCHAR(10)) AS Scale,
        CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable,
        CASE WHEN c.is_identity = 1 THEN 'YES' ELSE 'NO' END AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('UnitOfMeasure')
    ORDER BY c.column_id;
    
    SELECT TOP 10 * FROM UnitOfMeasure;
END

-- 5. OrderRequest table structure (referenced in stored procedures)
IF OBJECT_ID('OrderRequest', 'U') IS NOT NULL
BEGIN
    SELECT 
        'OrderRequest Columns' AS Info,
        c.name AS ColumnName,
        t.name AS DataType,
        CAST(c.max_length AS VARCHAR(10)) AS MaxLength,
        CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('OrderRequest')
    ORDER BY c.column_id;
    
    SELECT TOP 5 * FROM OrderRequest;
END

-- 6. Get all table names for analysis
SELECT name AS TableName
FROM sys.tables
WHERE schema_id = SCHEMA_ID('dbo')
ORDER BY name;




-- Comprehensive scan of EMR database structure and purpose
-- This script analyzes tables, views, stored procedures, and relationships

USE [EMR];
GO

PRINT '========================================';
PRINT 'EMR DATABASE SCAN';
PRINT '========================================';
PRINT '';

-- 1. List all tables with row counts
PRINT '1. TABLES IN EMR DATABASE:';
PRINT '----------------------------------------';
SELECT 
    t.name AS TableName,
    s.name AS SchemaName,
    p.rows AS RowCount,
    (SELECT SUM(a.total_pages) * 8 / 1024.0 
     FROM sys.tables t2
     INNER JOIN sys.indexes i ON t2.object_id = i.object_id
     INNER JOIN sys.partitions p2 ON i.object_id = p2.object_id AND i.index_id = p2.index_id
     INNER JOIN sys.allocation_units a ON p2.partition_id = a.container_id
     WHERE t2.name = t.name) AS SizeMB
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
WHERE s.name = 'dbo'
GROUP BY t.name, s.name, p.rows
ORDER BY t.name;
PRINT '';

-- 2. List all views
PRINT '2. VIEWS IN EMR DATABASE:';
PRINT '----------------------------------------';
SELECT 
    v.name AS ViewName,
    s.name AS SchemaName,
    OBJECT_DEFINITION(v.object_id) AS ViewDefinition
FROM sys.views v
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
WHERE s.name = 'dbo'
ORDER BY v.name;
PRINT '';

-- 3. List all stored procedures
PRINT '3. STORED PROCEDURES IN EMR DATABASE:';
PRINT '----------------------------------------';
SELECT 
    p.name AS ProcedureName,
    s.name AS SchemaName,
    p.create_date AS CreatedDate,
    p.modify_date AS ModifiedDate
FROM sys.procedures p
INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
WHERE s.name = 'dbo'
ORDER BY p.name;
PRINT '';

-- 4. List all functions
PRINT '4. FUNCTIONS IN EMR DATABASE:';
PRINT '----------------------------------------';
SELECT 
    o.name AS FunctionName,
    s.name AS SchemaName,
    o.type_desc AS FunctionType
FROM sys.objects o
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.type IN ('FN', 'IF', 'TF') -- Scalar, Inline Table, Table-valued functions
  AND s.name = 'dbo'
ORDER BY o.name;
PRINT '';

-- 5. Get detailed structure of key tables (UnitOfMeasure and others)
PRINT '5. DETAILED TABLE STRUCTURES:';
PRINT '----------------------------------------';

-- UnitOfMeasure table (known to be used)
IF OBJECT_ID('UnitOfMeasure', 'U') IS NOT NULL
BEGIN
    PRINT 'UnitOfMeasure Table:';
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.precision AS Precision,
        c.scale AS Scale,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity,
        CASE WHEN pk.column_id IS NOT NULL THEN 'YES' ELSE 'NO' END AS IsPrimaryKey,
        ISNULL(dc.definition, '') AS DefaultValue
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    LEFT JOIN (
        SELECT ic.object_id, ic.column_id
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        WHERE i.is_primary_key = 1
    ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
    LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('UnitOfMeasure')
    ORDER BY c.column_id;
    
    PRINT 'Sample UnitOfMeasure data:';
    SELECT TOP 10 * FROM UnitOfMeasure;
    PRINT '';
END

-- OrderRequest table (referenced in stored procedures)
IF OBJECT_ID('OrderRequest', 'U') IS NOT NULL
BEGIN
    PRINT 'OrderRequest Table Structure:';
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('OrderRequest')
    ORDER BY c.column_id;
    
    PRINT 'Sample OrderRequest data:';
    SELECT TOP 5 * FROM OrderRequest;
    PRINT '';
END

-- 6. Check for foreign key relationships
PRINT '6. FOREIGN KEY RELATIONSHIPS:';
PRINT '----------------------------------------';
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS ParentTable,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ParentColumn,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fc ON fk.object_id = fc.constraint_object_id
ORDER BY ParentTable, ForeignKeyName;
PRINT '';

-- 7. Check for indexes
PRINT '7. INDEXES (Top 20 by table):';
PRINT '----------------------------------------';
SELECT TOP 20
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.is_primary_key AS IsPrimaryKey,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS IndexColumns
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id > 100 -- User tables only
  AND i.type > 0 -- Exclude heap
GROUP BY i.object_id, i.name, i.type_desc, i.is_unique, i.is_primary_key
ORDER BY TableName, IndexName;
PRINT '';

-- 8. Get table names that might indicate purpose
PRINT '8. TABLE NAMES ANALYSIS (to understand purpose):';
PRINT '----------------------------------------';
SELECT name AS TableName
FROM sys.tables
WHERE schema_id = SCHEMA_ID('dbo')
ORDER BY name;
PRINT '';

-- 9. Check for any cross-database references FROM EMR
PRINT '9. CHECKING FOR CROSS-DATABASE REFERENCES:';
PRINT '----------------------------------------';
-- This would require checking stored procedures and views for references to other databases
SELECT 
    p.name AS ProcedureName,
    OBJECT_DEFINITION(p.object_id) AS Definition
FROM sys.procedures p
WHERE OBJECT_DEFINITION(p.object_id) LIKE '%[[]%[]].[[]dbo[]].[[]%'
   OR OBJECT_DEFINITION(p.object_id) LIKE '%.dbo.%'
ORDER BY p.name;
PRINT '';

-- 10. Summary statistics
PRINT '10. DATABASE SUMMARY:';
PRINT '----------------------------------------';
SELECT 
    (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')) AS TotalTables,
    (SELECT COUNT(*) FROM sys.views WHERE schema_id = SCHEMA_ID('dbo')) AS TotalViews,
    (SELECT COUNT(*) FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo')) AS TotalStoredProcedures,
    (SELECT COUNT(*) FROM sys.objects WHERE type IN ('FN', 'IF', 'TF') AND schema_id = SCHEMA_ID('dbo')) AS TotalFunctions;
PRINT '';

PRINT '========================================';
PRINT 'SCAN COMPLETE';
PRINT '========================================';




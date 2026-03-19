-- =====================================================
-- Scan Billing Database Tables
-- Tables: InvoiceHeader, InvoiceDetail
-- =====================================================

USE Billing;
GO

PRINT '========================================';
PRINT 'SCANNING BILLING DATABASE TABLES';
PRINT '========================================';
PRINT '';

-- =====================================================
-- 1. INVOICEHEADER TABLE STRUCTURE
-- =====================================================
PRINT '1. InvoiceHeader - Table Structure';
PRINT '------------------------------------';

SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'InvoiceHeader'
ORDER BY c.ORDINAL_POSITION;

PRINT '';
PRINT '2. InvoiceHeader - Primary Keys';
PRINT '------------------------------------';

SELECT 
    kcu.COLUMN_NAME,
    tc.CONSTRAINT_TYPE
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE tc.TABLE_NAME = 'InvoiceHeader'
    AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY';

PRINT '';
PRINT '3. InvoiceHeader - Foreign Keys';
PRINT '------------------------------------';

SELECT 
    fk.name AS ForeignKey_Name,
    OBJECT_NAME(fk.parent_object_id) AS Table_Name,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS Column_Name,
    OBJECT_NAME(fk.referenced_object_id) AS Referenced_Table,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS Referenced_Column
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc 
    ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) = 'InvoiceHeader';

PRINT '';
PRINT '4. InvoiceHeader - Sample Data (First 10 rows)';
PRINT '------------------------------------';

SELECT TOP 10 * 
FROM InvoiceHeader
ORDER BY 1 DESC;

PRINT '';
PRINT '5. InvoiceHeader - Row Count';
PRINT '------------------------------------';

SELECT COUNT(*) AS Total_Rows
FROM InvoiceHeader;

PRINT '';
PRINT '';

-- =====================================================
-- 6. INVOICEDETAIL TABLE STRUCTURE
-- =====================================================
PRINT '6. InvoiceDetail - Table Structure';
PRINT '------------------------------------';

SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'InvoiceDetail'
ORDER BY c.ORDINAL_POSITION;

PRINT '';
PRINT '7. InvoiceDetail - Primary Keys';
PRINT '------------------------------------';

SELECT 
    kcu.COLUMN_NAME,
    tc.CONSTRAINT_TYPE
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE tc.TABLE_NAME = 'InvoiceDetail'
    AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY';

PRINT '';
PRINT '8. InvoiceDetail - Foreign Keys';
PRINT '------------------------------------';

SELECT 
    fk.name AS ForeignKey_Name,
    OBJECT_NAME(fk.parent_object_id) AS Table_Name,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS Column_Name,
    OBJECT_NAME(fk.referenced_object_id) AS Referenced_Table,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS Referenced_Column
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc 
    ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) = 'InvoiceDetail';

PRINT '';
PRINT '9. InvoiceDetail - Sample Data (First 10 rows)';
PRINT '------------------------------------';

SELECT TOP 10 * 
FROM InvoiceDetail
ORDER BY 1 DESC;

PRINT '';
PRINT '10. InvoiceDetail - Row Count';
PRINT '------------------------------------';

SELECT COUNT(*) AS Total_Rows
FROM InvoiceDetail;

PRINT '';
PRINT '';

-- =====================================================
-- 11. RELATIONSHIP ANALYSIS
-- =====================================================
PRINT '11. Relationship Analysis';
PRINT '------------------------------------';
PRINT 'Checking relationship between InvoiceHeader and InvoiceDetail...';

-- Check common columns that might link the tables
SELECT 
    'InvoiceHeader Columns' AS Source,
    COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceHeader'
    AND COLUMN_NAME LIKE '%ID%'
UNION ALL
SELECT 
    'InvoiceDetail Columns' AS Source,
    COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceDetail'
    AND COLUMN_NAME LIKE '%ID%' OR COLUMN_NAME LIKE '%Invoice%'
ORDER BY Source, COLUMN_NAME;

PRINT '';
PRINT '12. Sample Join Preview';
PRINT '------------------------------------';

-- Try to find the relationship by joining on common ID columns
SELECT TOP 5
    h.*,
    d.*
FROM InvoiceHeader h
LEFT JOIN InvoiceDetail d ON h.ID = d.InvoiceHeaderID
ORDER BY h.ID DESC;

PRINT '';
PRINT '========================================';
PRINT 'SCAN COMPLETE';
PRINT '========================================';























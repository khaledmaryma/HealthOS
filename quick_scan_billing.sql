-- =====================================================
-- Quick Scan of Billing Database Tables
-- =====================================================

USE Billing;
GO

-- InvoiceHeader Structure
PRINT '=== InvoiceHeader Structure ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceHeader'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '=== InvoiceHeader Sample (5 rows) ===';
SELECT TOP 5 * FROM InvoiceHeader ORDER BY ID DESC;

PRINT '';
PRINT '=== InvoiceHeader Row Count ===';
SELECT COUNT(*) AS Total_Rows FROM InvoiceHeader;

PRINT '';
PRINT '';

-- InvoiceDetail Structure
PRINT '=== InvoiceDetail Structure ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceDetail'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '=== InvoiceDetail Sample (5 rows) ===';
SELECT TOP 5 * FROM InvoiceDetail ORDER BY ID DESC;

PRINT '';
PRINT '=== InvoiceDetail Row Count ===';
SELECT COUNT(*) AS Total_Rows FROM InvoiceDetail;

PRINT '';
PRINT '=== Relationship Check ===';
-- Show columns that might link the tables
SELECT 'Header ID Columns:' AS Info, STRING_AGG(COLUMN_NAME, ', ') AS Columns
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceHeader' AND COLUMN_NAME LIKE '%ID%'
UNION ALL
SELECT 'Detail ID Columns:', STRING_AGG(COLUMN_NAME, ', ')
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceDetail' AND (COLUMN_NAME LIKE '%ID%' OR COLUMN_NAME LIKE '%Invoice%');























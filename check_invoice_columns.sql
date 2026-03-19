-- Quick check of actual column names in InvoiceHeader and InvoiceDetail tables
USE Billing;
GO

PRINT '========================================';
PRINT 'InvoiceHeader Columns:';
PRINT '========================================';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceHeader'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'InvoiceDetail Columns:';
PRINT '========================================';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceDetail'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'Sample InvoiceHeader data (top 2):';
PRINT '========================================';
SELECT TOP 2 * FROM InvoiceHeader WHERE AdmissionNumber IS NOT NULL ORDER BY ID DESC;

PRINT '';
PRINT '========================================';
PRINT 'Sample InvoiceDetail data (top 2):';
PRINT '========================================';
SELECT TOP 2 * FROM InvoiceDetail WHERE CostCenter = 1 ORDER BY ID DESC;


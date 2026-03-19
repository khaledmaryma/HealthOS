USE Configuration;
GO

-- Check TransactionSequenceControl structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TransactionSequenceControl'
ORDER BY ORDINAL_POSITION;

-- Check current values
SELECT *
FROM TransactionSequenceControl WITH (NOLOCK);





















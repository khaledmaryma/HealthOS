-- Scan CommonDefinition tables for autocomplete

USE CommonDefinition;
GO

-- Name table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Name'
ORDER BY ORDINAL_POSITION;

-- Sample Name data
SELECT TOP 20 *
FROM Name WITH (NOLOCK)
WHERE IsDeleted = 0
ORDER BY Name;

-- Family table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Family'
ORDER BY ORDINAL_POSITION;

-- Sample Family data
SELECT TOP 20 *
FROM Family WITH (NOLOCK)
WHERE IsDeleted = 0
ORDER BY Name;





















-- =====================================================
-- Script to Set Database Compatibility Level for OPENJSON Support
-- OPENJSON requires SQL Server 2016+ with compatibility level 130+
-- =====================================================

-- Step 1: Check current compatibility level
SELECT 
    name AS DatabaseName,
    compatibility_level AS CurrentCompatibilityLevel,
    CASE compatibility_level
        WHEN 90 THEN 'SQL Server 2005 (9.0)'
        WHEN 100 THEN 'SQL Server 2008 (10.0)'
        WHEN 110 THEN 'SQL Server 2012 (11.0)'
        WHEN 120 THEN 'SQL Server 2014 (12.0)'
        WHEN 130 THEN 'SQL Server 2016 (13.0) - Supports OPENJSON'
        WHEN 140 THEN 'SQL Server 2017 (14.0) - Supports OPENJSON'
        WHEN 150 THEN 'SQL Server 2019 (15.0) - Supports OPENJSON'
        WHEN 160 THEN 'SQL Server 2022 (16.0) - Supports OPENJSON'
        ELSE 'Unknown version'
    END AS VersionDescription
FROM sys.databases
WHERE name = 'Admission';

-- Step 2: Check SQL Server version to determine maximum compatibility level
SELECT 
    @@VERSION AS SQLServerVersion,
    SERVERPROPERTY('ProductVersion') AS ProductVersion,
    SERVERPROPERTY('ProductLevel') AS ProductLevel,
    SERVERPROPERTY('Edition') AS Edition;

-- Step 3: Set compatibility level to 130 (SQL Server 2016)
-- Note: You must be connected to the master database or have ALTER DATABASE permission
-- Change the compatibility level based on your SQL Server version:
--   SQL Server 2016: 130
--   SQL Server 2017: 140
--   SQL Server 2019: 150
--   SQL Server 2022: 160

-- Option A: Set to 130 (minimum for OPENJSON)
USE master;
GO
ALTER DATABASE Admission SET COMPATIBILITY_LEVEL = 130;
GO

-- Option B: Set to highest available for your SQL Server version
-- (Uncomment the one that matches your SQL Server version)

-- For SQL Server 2016:
-- ALTER DATABASE Admission SET COMPATIBILITY_LEVEL = 130;
-- GO

-- For SQL Server 2017:
-- ALTER DATABASE Admission SET COMPATIBILITY_LEVEL = 140;
-- GO

-- For SQL Server 2019:
-- ALTER DATABASE Admission SET COMPATIBILITY_LEVEL = 150;
-- GO

-- For SQL Server 2022:
-- ALTER DATABASE Admission SET COMPATIBILITY_LEVEL = 160;
-- GO

-- Step 4: Verify the change
SELECT 
    name AS DatabaseName,
    compatibility_level AS NewCompatibilityLevel,
    CASE compatibility_level
        WHEN 130 THEN 'SQL Server 2016 (13.0) - OPENJSON Supported ✓'
        WHEN 140 THEN 'SQL Server 2017 (14.0) - OPENJSON Supported ✓'
        WHEN 150 THEN 'SQL Server 2019 (15.0) - OPENJSON Supported ✓'
        WHEN 160 THEN 'SQL Server 2022 (16.0) - OPENJSON Supported ✓'
        ELSE 'OPENJSON may not be available'
    END AS Status
FROM sys.databases
WHERE name = 'Admission';

-- Step 5: Test OPENJSON functionality (optional)
-- This will verify that OPENJSON is now available
USE Admission;
GO

DECLARE @testJson NVARCHAR(MAX) = N'[{"id":1,"name":"Test"}]';
SELECT * FROM OPENJSON(@testJson) WITH (id INT, name NVARCHAR(50));
GO



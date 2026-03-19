-- Update ResultType in PatientLabResult table based on LabTest master data
-- This script syncs the ResultType from LabTest to PatientLabResult records

PRINT '====================================================================';
PRINT 'Setting ResultType in PatientLabResult from LabTest master table';
PRINT 'ResultType: 1 = Text, 2 = Numeric, 3 = Antibiogram';
PRINT '====================================================================';
PRINT '';

-- First, ensure LabTestID 136 has ResultType = 3 in the master table
PRINT '1. Setting LabTestID 136 to ResultType = 3 (Antibiogram)';
PRINT '-------------------------------------------------------------------';

UPDATE LabTest
SET ResultType = 3,
    ModifiedDate = GETDATE()
WHERE ID = 136
    AND IsDeleted = 0;

PRINT '✓ Updated LabTest ID 136 to ResultType = 3';
PRINT '';

-- Update all PatientLabResult records to match their LabTest ResultType
PRINT '2. Syncing ResultType from LabTest to PatientLabResult';
PRINT '-------------------------------------------------------------------';

DECLARE @UpdatedCount INT = 0;

BEGIN TRANSACTION;

UPDATE plr
SET plr.ResultType = lt.ResultType,
    plr.ModifiedDate = GETDATE()
FROM PatientLabResult plr
INNER JOIN LabTest lt ON plr.LabTestID = lt.ID
WHERE plr.IsDeleted = 0
    AND lt.IsDeleted = 0
    AND (plr.ResultType IS NULL OR plr.ResultType <> lt.ResultType);

SET @UpdatedCount = @@ROWCOUNT;

COMMIT TRANSACTION;

PRINT '✓ Updated ' + CAST(@UpdatedCount AS VARCHAR(10)) + ' PatientLabResult records';
PRINT '';

-- Show the distribution
PRINT '3. ResultType Distribution in PatientLabResult:';
PRINT '-------------------------------------------------------------------';

SELECT 
    CASE ResultType
        WHEN 1 THEN 'Text'
        WHEN 2 THEN 'Numeric'
        WHEN 3 THEN 'Antibiogram'
        ELSE 'NULL/Unknown'
    END AS ResultTypeDescription,
    ResultType,
    COUNT(*) AS Count
FROM PatientLabResult
WHERE IsDeleted = 0
GROUP BY ResultType
ORDER BY ResultType;

PRINT '';

-- Show LabTestID 136 records
PRINT '4. PatientLabResult records for LabTestID 136 (should be ResultType = 3):';
PRINT '-------------------------------------------------------------------';

SELECT 
    plr.ID,
    plr.PatientHeaderID,
    plr.LabTestID,
    plr.ResultType,
    plr.LabTestDescription,
    plrh.AdmissionNumber,
    CASE plr.ResultType
        WHEN 3 THEN '✓ Correct (Antibiogram)'
        ELSE '✗ Wrong - should be 3'
    END AS ValidationStatus
FROM PatientLabResult plr
LEFT JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
WHERE plr.LabTestID = 136
    AND plr.IsDeleted = 0
ORDER BY plr.ID;

PRINT '';
PRINT '====================================================================';
PRINT 'COMPLETED';
PRINT '====================================================================';






























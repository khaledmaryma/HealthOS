-- Detailed diagnostic for admission 03.01254.08.24
-- This patient has Antibiogram but it's not appearing in separated section

DECLARE @AdmissionNumber NVARCHAR(20) = '03.01254.08.24';

PRINT '=== PATIENT LAB RESULTS WITH RESULTTYPE ==='
PRINT ''

SELECT 
    plr.ID AS PatientLabResultID,
    plr.LabTestID,
    plr.LabTestDescription,
    lt.TestDesciption AS LabTestMasterDescription,
    lt.ResultType,
    CASE 
        WHEN lt.ResultType IS NULL THEN '❌ NULL - NOT SET'
        WHEN lt.ResultType = 1 THEN '✅ 1 - Text Result'
        WHEN lt.ResultType = 2 THEN '✅ 2 - Numeric Result'
        WHEN lt.ResultType = 3 THEN '✅ 3 - Antibiogram'
        ELSE '⚠️ ' + CAST(lt.ResultType AS NVARCHAR(10)) + ' - Unknown'
    END AS ResultTypeStatus,
    plr.Result,
    plr.DefaultTextResult,
    plr.DisplayOrder,
    plr.MedicalClassDesc
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
LEFT JOIN LabTest lt ON plr.LabTestID = lt.ID
WHERE plrh.AdmissionNumber = @AdmissionNumber
  AND plrh.IsDeleted = 0
  AND plr.IsDeleted = 0
ORDER BY plr.DisplayOrder, plr.MedicalClassDesc;

PRINT ''
PRINT '=== SUMMARY BY RESULTTYPE ==='
PRINT ''

SELECT 
    CASE 
        WHEN lt.ResultType IS NULL THEN 'NULL (❌ Not Set)'
        WHEN lt.ResultType = 1 THEN '1 (Text)'
        WHEN lt.ResultType = 2 THEN '2 (Numeric)'
        WHEN lt.ResultType = 3 THEN '3 (Antibiogram)'
        ELSE CAST(lt.ResultType AS NVARCHAR(10)) + ' (Unknown)'
    END AS ResultType,
    COUNT(*) AS Count,
    STRING_AGG(plr.LabTestDescription, ', ') AS Tests
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
LEFT JOIN LabTest lt ON plr.LabTestID = lt.ID
WHERE plrh.AdmissionNumber = @AdmissionNumber
  AND plrh.IsDeleted = 0
  AND plr.IsDeleted = 0
GROUP BY lt.ResultType
ORDER BY lt.ResultType;

PRINT ''
PRINT '=== WHICH TESTS ARE ANTIBIOGRAM? ==='
PRINT ''

-- Check if any tests have "Antibiogram" in the name
SELECT 
    plr.ID,
    plr.LabTestID,
    plr.LabTestDescription,
    lt.ResultType AS CurrentResultType,
    CASE 
        WHEN plr.LabTestDescription LIKE '%Antibiogram%' 
             OR plr.LabTestDescription LIKE '%Bacteriology%'
             OR plr.LabTestDescription LIKE '%Culture%'
             OR plr.LabTestDescription LIKE '%Sensitivity%'
        THEN '🦠 YES - Should be ResultType = 3'
        ELSE 'No'
    END AS IsAntibiogramByName,
    CASE 
        WHEN plr.LabTestID = 136 THEN '🦠 YES - LabTestID 136'
        ELSE 'No'
    END AS IsAntibiogramByID
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
LEFT JOIN LabTest lt ON plr.LabTestID = lt.ID
WHERE plrh.AdmissionNumber = @AdmissionNumber
  AND plrh.IsDeleted = 0
  AND plr.IsDeleted = 0
ORDER BY plr.DisplayOrder;

PRINT ''
PRINT '=== FIX: UPDATE RESULTTYPE FOR ANTIBIOGRAM TESTS ==='
PRINT ''
PRINT 'If any tests above should be Antibiogram (ResultType = 3), run:'
PRINT ''
PRINT '-- Update the LabTest master table to set ResultType = 3'
PRINT 'UPDATE LabTest'
PRINT 'SET ResultType = 3,'
PRINT '    ModifiedDate = GETDATE()'
PRINT 'WHERE ID IN (SELECT DISTINCT LabTestID FROM ... WHERE conditions match antibiogram tests)'
PRINT ''






























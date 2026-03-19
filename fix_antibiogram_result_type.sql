-- Script to identify and fix Antibiogram tests that don't have ResultType = 3

PRINT '=== STEP 1: Check current ResultType for common Antibiogram tests ==='
PRINT ''

SELECT 
    ID,
    TestDesciption,
    ResultType,
    CASE 
        WHEN ResultType IS NULL THEN '❌ NULL - Needs to be set to 3'
        WHEN ResultType = 3 THEN '✅ Already correct (3 = Antibiogram)'
        ELSE '⚠️ Currently ' + CAST(ResultType AS NVARCHAR(10)) + ' - Should be 3'
    END AS Status
FROM LabTest
WHERE (
    TestDesciption LIKE '%Antibiogram%'
    OR TestDesciption LIKE '%Bacteriology%'
    OR TestDesciption LIKE '%Culture%'
    OR TestDesciption LIKE '%Antibiotic%Sensitivity%'
    OR TestDesciption LIKE '%Germ%'
    OR ID = 136  -- The specific Antibiogram test ID
)
AND IsDeleted = 0
ORDER BY ID;

PRINT ''
PRINT '=== STEP 2: Check if admission 03.01254.08.24 has any of these tests ==='
PRINT ''

SELECT 
    plr.ID AS PatientLabResultID,
    plr.LabTestID,
    plr.LabTestDescription,
    lt.TestDesciption AS MasterTestDescription,
    lt.ResultType AS CurrentResultType,
    CASE 
        WHEN lt.ResultType = 3 THEN '✅ Already correct'
        ELSE '❌ Needs update to 3'
    END AS NeedsUpdate
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
LEFT JOIN LabTest lt ON plr.LabTestID = lt.ID
WHERE plrh.AdmissionNumber = '03.01254.08.24'
  AND plrh.IsDeleted = 0
  AND plr.IsDeleted = 0
  AND (
      plr.LabTestDescription LIKE '%Antibiogram%'
      OR plr.LabTestDescription LIKE '%Bacteriology%'
      OR plr.LabTestDescription LIKE '%Culture%'
      OR plr.LabTestID = 136
  )
ORDER BY plr.DisplayOrder;

PRINT ''
PRINT '=== STEP 3: FIX - Update ResultType to 3 for Antibiogram tests ==='
PRINT ''
PRINT 'Review the results above, then uncomment and run the UPDATE statement below:'
PRINT ''

/*
-- UPDATE: Set ResultType = 3 for Antibiogram/Bacteriology tests
UPDATE LabTest
SET ResultType = 3,
    ModifiedDate = GETDATE()
WHERE (
    TestDesciption LIKE '%Antibiogram%'
    OR TestDesciption LIKE '%Bacteriology%'
    OR TestDesciption LIKE '%Culture%'
    OR TestDesciption LIKE '%Antibiotic%Sensitivity%'
    OR TestDesciption LIKE '%Germ%'
    OR ID = 136  -- Specific Antibiogram test
)
AND IsDeleted = 0
AND (ResultType IS NULL OR ResultType != 3);  -- Only update if not already set to 3

-- Show updated records
SELECT 
    ID,
    TestDesciption,
    ResultType,
    '✅ Updated to 3 (Antibiogram)' AS Status
FROM LabTest
WHERE (
    TestDesciption LIKE '%Antibiogram%'
    OR TestDesciption LIKE '%Bacteriology%'
    OR TestDesciption LIKE '%Culture%'
    OR TestDesciption LIKE '%Antibiotic%Sensitivity%'
    OR ID = 136
)
AND IsDeleted = 0
AND ResultType = 3;
*/

PRINT ''
PRINT '=== ALTERNATIVE: Update specific LabTestID ==='
PRINT ''
PRINT 'If you know the specific LabTestID(s), use this:'
PRINT ''
PRINT '-- Example: Update LabTestID 136 to be Antibiogram'
PRINT 'UPDATE LabTest SET ResultType = 3, ModifiedDate = GETDATE() WHERE ID = 136 AND IsDeleted = 0;'
PRINT ''






























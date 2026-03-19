-- Check ResultType values for a specific patient's lab results
-- This helps diagnose why results aren't being separated correctly

DECLARE @AdmissionNumber NVARCHAR(20) = '03.01254.08.24'; -- Change this to the patient you're viewing

SELECT 
    plr.ID AS PatientLabResultID,
    plr.LabTestID,
    plr.LabTestDescription,
    lt.ResultType,
    CASE 
        WHEN lt.ResultType IS NULL THEN 'NULL (No ResultType)'
        WHEN lt.ResultType = 1 THEN 'Text Result'
        WHEN lt.ResultType = 2 THEN 'Numeric Result'
        WHEN lt.ResultType = 3 THEN 'Antibiogram'
        ELSE 'Unknown Type: ' + CAST(lt.ResultType AS NVARCHAR(10))
    END AS ResultTypeDescription,
    plr.Result,
    plr.DefaultTextResult
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
LEFT JOIN LabTest lt ON plr.LabTestID = lt.ID
WHERE plrh.AdmissionNumber = @AdmissionNumber
  AND plrh.IsDeleted = 0
  AND plr.IsDeleted = 0
ORDER BY plr.DisplayOrder, plr.MedicalClassDesc;

-- Summary count by ResultType
SELECT 
    lt.ResultType,
    CASE 
        WHEN lt.ResultType IS NULL THEN 'NULL (No ResultType)'
        WHEN lt.ResultType = 1 THEN 'Text Result'
        WHEN lt.ResultType = 2 THEN 'Numeric Result'
        WHEN lt.ResultType = 3 THEN 'Antibiogram'
        ELSE 'Unknown Type: ' + CAST(lt.ResultType AS NVARCHAR(10))
    END AS ResultTypeDescription,
    COUNT(*) AS Count
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
LEFT JOIN LabTest lt ON plr.LabTestID = lt.ID
WHERE plrh.AdmissionNumber = @AdmissionNumber
  AND plrh.IsDeleted = 0
  AND plr.IsDeleted = 0
GROUP BY lt.ResultType
ORDER BY lt.ResultType;

-- Check if any LabTests are missing ResultType
SELECT 
    lt.ID,
    lt.TestDesciption,
    lt.ResultType,
    CASE 
        WHEN lt.ResultType IS NULL THEN '⚠️ MISSING - Should be set to 1, 2, or 3'
        ELSE '✓ OK'
    END AS Status
FROM LabTest lt
WHERE lt.ID IN (
    SELECT DISTINCT plr.LabTestID
    FROM PatientLabResult plr
    INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
    WHERE plrh.AdmissionNumber = @AdmissionNumber
      AND plrh.IsDeleted = 0
      AND plr.IsDeleted = 0
)
  AND lt.IsDeleted = 0
ORDER BY lt.ResultType, lt.TestDesciption;






























-- Check what lab tests exist for patient 03.01186.08.24
-- and look for any that might be Antibiogram-related

DECLARE @AdmissionNumber VARCHAR(20) = '03.01186.08.24';

-- Get all lab tests for this patient
SELECT 
    plr.ID,
    plr.LabTestID,
    plr.LabTestDescription,
    plr.MedicalClassDesc,
    plr.Result,
    plr.DefaultTextResult,
    CASE 
        WHEN LOWER(plr.LabTestDescription) LIKE '%antibiogram%' THEN 'YES - Contains antibiogram'
        WHEN LOWER(plr.LabTestDescription) LIKE '%bacteriology%' THEN 'YES - Contains bacteriology'
        WHEN LOWER(plr.LabTestDescription) LIKE '%culture%' THEN 'YES - Contains culture'
        WHEN LOWER(plr.LabTestDescription) LIKE '%sensitivity%' THEN 'YES - Contains sensitivity'
        ELSE 'NO - Regular test'
    END AS IsAntibiogramRelated
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
WHERE plrh.AdmissionNumber = @AdmissionNumber
    AND plr.IsDeleted = 0
    AND plrh.IsDeleted = 0
ORDER BY plr.DisplayOrder, plr.MedicalClassDesc;

-- Check if there are any lab tests with "Antibiogram" in their name in the entire database
PRINT '';
PRINT '======================================================================';
PRINT 'All lab tests in the system that contain "Antibiogram" or similar:';
PRINT '======================================================================';

SELECT 
    ID,
    TestDesciption,
    MedicalClassDescription,
    ResultType,
    CASE ResultType
        WHEN 1 THEN 'Numeric'
        WHEN 2 THEN 'Text'
        ELSE 'Unknown'
    END AS ResultTypeName
FROM LabTest
WHERE IsDeleted = 0
    AND (
        LOWER(TestDesciption) LIKE '%antibiogram%'
        OR LOWER(TestDesciption) LIKE '%bacteriology%'
        OR LOWER(TestDesciption) LIKE '%culture%'
        OR LOWER(TestDesciption) LIKE '%sensitivity%'
    )
ORDER BY TestDesciption;






























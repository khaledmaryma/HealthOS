-- Check if patient 03.01186.08.24 has LabTestID 136 (Antibiogram)

DECLARE @AdmissionNumber VARCHAR(20) = '03.01186.08.24';
DECLARE @AntibiogramLabTestID INT = 136;

PRINT '====================================================================';
PRINT 'Checking for LabTestID 136 (Antibiogram) for patient ' + @AdmissionNumber;
PRINT '====================================================================';
PRINT '';

-- Check if the patient has this lab test
SELECT 
    plr.ID,
    plr.LabTestID,
    plr.LabTestDescription,
    plr.MedicalClassDesc,
    plr.Result,
    plr.ResultDate,
    plr.CreatedDate,
    CASE 
        WHEN plr.LabTestID = @AntibiogramLabTestID THEN 'YES - This is the Antibiogram test'
        ELSE 'NO - Different test'
    END AS IsAntibiogramTest
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
WHERE plrh.AdmissionNumber = @AdmissionNumber
    AND plr.IsDeleted = 0
    AND plrh.IsDeleted = 0
ORDER BY plr.DisplayOrder;

PRINT '';
PRINT '====================================================================';

-- Count results
DECLARE @HasAntibiogram BIT = 0;

IF EXISTS (
    SELECT 1 
    FROM PatientLabResult plr
    INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
    WHERE plrh.AdmissionNumber = @AdmissionNumber
        AND plr.LabTestID = @AntibiogramLabTestID
        AND plr.IsDeleted = 0
        AND plrh.IsDeleted = 0
)
BEGIN
    SET @HasAntibiogram = 1;
    PRINT '✓ Patient HAS LabTestID 136 (Antibiogram) - Section will show';
END
ELSE
BEGIN
    PRINT '✗ Patient DOES NOT have LabTestID 136 (Antibiogram) - Section will NOT show';
    PRINT '';
    PRINT 'To add the Antibiogram test to this patient, run:';
    PRINT '';
    PRINT 'INSERT INTO PatientLabResult (PatientHeaderID, LabTestID, LabTestDescription, IsDeleted, CreatedBy, CreatedDate, ...)';
    PRINT 'SELECT plrh.ID, 136, lt.TestDesciption, 0, 1, GETDATE(), ...';
    PRINT 'FROM PatientLabResultsHeader plrh';
    PRINT 'CROSS JOIN LabTest lt';
    PRINT 'WHERE plrh.AdmissionNumber = ''' + @AdmissionNumber + '''';
    PRINT '  AND lt.ID = 136;';
END

PRINT '====================================================================';

-- Show what LabTestID 136 is in the master table
PRINT '';
PRINT 'LabTest master data for ID 136:';
PRINT '====================================================================';

SELECT 
    ID,
    TestDesciption,
    MedicalClassDescription,
    ResultType,
    CASE ResultType
        WHEN 1 THEN 'Numeric'
        WHEN 2 THEN 'Text'
        ELSE 'Unknown'
    END AS ResultTypeName,
    DisplayOrder,
    IsDeleted
FROM LabTest
WHERE ID = 136;






























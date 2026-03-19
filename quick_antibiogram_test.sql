-- Quick Test: Check Antibiogram Status for Patient 03.01186.08.24
-- Run this script to see exactly what's happening

DECLARE @AdmissionNumber VARCHAR(20) = '03.01186.08.24';

PRINT '====================================================================';
PRINT 'ANTIBIOGRAM TEST REPORT FOR PATIENT ' + @AdmissionNumber;
PRINT '====================================================================';
PRINT '';

-- 1. Check what lab tests this patient has
PRINT '1. ALL LAB TESTS FOR THIS PATIENT:';
PRINT '-------------------------------------------------------------------';

SELECT 
    plr.ID,
    plr.LabTestID,
    plr.LabTestDescription,
    plr.MedicalClassDesc,
    CASE 
        WHEN plr.LabTestID = 136 THEN '★ ANTIBIOGRAM ★'
        ELSE ''
    END AS IsAntibiogram
FROM PatientLabResult plr
INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
WHERE plrh.AdmissionNumber = @AdmissionNumber
    AND plr.IsDeleted = 0
    AND plrh.IsDeleted = 0
ORDER BY plr.LabTestID;

PRINT '';

-- 2. Specific check for LabTestID 136
PRINT '2. CHECK FOR LABTESTID 136:';
PRINT '-------------------------------------------------------------------';

IF EXISTS (
    SELECT 1 
    FROM PatientLabResult plr
    INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
    WHERE plrh.AdmissionNumber = @AdmissionNumber
        AND plr.LabTestID = 136
        AND plr.IsDeleted = 0
)
BEGIN
    PRINT '✓✓✓ YES - Patient HAS LabTestID 136';
    PRINT '';
    
    SELECT 
        'Patient SHOULD see Antibiogram section' AS Status,
        plr.ID AS PatientLabResultID,
        plr.LabTestID,
        plr.LabTestDescription,
        plr.Result AS SpecimenType,
        plr.CreatedDate
    FROM PatientLabResult plr
    INNER JOIN PatientLabResultsHeader plrh ON plr.PatientHeaderID = plrh.ID
    WHERE plrh.AdmissionNumber = @AdmissionNumber
        AND plr.LabTestID = 136
        AND plr.IsDeleted = 0;
END
ELSE
BEGIN
    PRINT '✗✗✗ NO - Patient DOES NOT have LabTestID 136';
    PRINT 'Antibiogram section will NOT show';
    PRINT '';
    PRINT 'TO FIX: Run this command:';
    PRINT '';
    
    -- Show the exact command to add it
    DECLARE @PatientHeaderID INT;
    SELECT @PatientHeaderID = ID 
    FROM PatientLabResultsHeader 
    WHERE AdmissionNumber = @AdmissionNumber AND IsDeleted = 0;
    
    IF @PatientHeaderID IS NOT NULL
    BEGIN
        PRINT '-- Copy and run this:';
        PRINT 'INSERT INTO PatientLabResult (PatientHeaderID, LabTestID, LabTestDescription, IsDeleted, CreatedBy, CreatedDate, DisplayOrder)';
        PRINT 'SELECT ' + CAST(@PatientHeaderID AS VARCHAR) + ', 136, TestDesciption, 0, 1, GETDATE(), DisplayOrder';
        PRINT 'FROM LabTest WHERE ID = 136 AND IsDeleted = 0;';
    END
    ELSE
    BEGIN
        PRINT '✗ ERROR: PatientLabResultsHeader not found for this admission number!';
    END
END

PRINT '';
PRINT '====================================================================';
PRINT '3. WHAT IS LABTESTID 136?';
PRINT '-------------------------------------------------------------------';

SELECT 
    ID AS LabTestID,
    TestDesciption,
    MedicalClassDescription,
    ResultType,
    CASE ResultType WHEN 1 THEN 'Numeric' WHEN 2 THEN 'Text' ELSE 'Unknown' END AS ResultTypeName,
    DisplayOrder,
    DefaultTextResult,
    IsDeleted,
    CASE IsDeleted WHEN 0 THEN 'Active' ELSE 'Deleted' END AS Status
FROM LabTest
WHERE ID = 136;

PRINT '';
PRINT '====================================================================';
PRINT 'END OF REPORT';
PRINT '====================================================================';






























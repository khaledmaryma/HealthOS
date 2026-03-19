-- Preview Missing Lab Tests for a Patient
-- This script shows which lab tests exist for other patients but not for the target patient
-- WITHOUT making any changes to the database

-- ===== CONFIGURATION =====
DECLARE @TargetPatientHeaderID INT = 108712;  -- Change this to your patient ID

-- ===== PREVIEW QUERY =====
PRINT 'Preview of lab tests missing for Patient Header ID: ' + CAST(@TargetPatientHeaderID AS VARCHAR(10));
PRINT '==================================================================================';

-- Show the missing lab tests with details
SELECT 
    lt.ID AS LabTestID,
    lt.TestDesciption AS LabTestDescription,
    lt.MedicalClassDescription AS MedicalClass,
    lt.DisplayOrder,
    lt.ResultType,
    CASE lt.ResultType
        WHEN 1 THEN 'Numeric'
        WHEN 2 THEN 'Text'
        ELSE 'Unknown'
    END AS ResultTypeName,
    lt.DefaultTextResult,
    CASE 
        WHEN lt.MaleNormalMin IS NOT NULL AND lt.MaleNormalMax IS NOT NULL 
        THEN CONCAT(CAST(lt.MaleNormalMin AS VARCHAR), ' - ', CAST(lt.MaleNormalMax AS VARCHAR))
        ELSE NULL 
    END AS NormalRange,
    uom.Description AS UnitOfMeasure,
    lt.Prefix,
    lt.Suffix,
    COUNT(DISTINCT pr.PatientHeaderID) AS ExistsForPatientCount
FROM LabTest lt
LEFT JOIN EMR.dbo.UnitOfMeasure uom ON lt.UOM = uom.ID
INNER JOIN PatientLabResult pr ON pr.LabTestID = lt.ID AND pr.IsDeleted = 0
WHERE lt.IsDeleted = 0
    AND lt.ID IN (
        -- Get distinct LabTestIDs that exist for other patients but not for target patient
        SELECT DISTINCT LabTestID
        FROM PatientLabResult
        WHERE IsDeleted = 0 
            AND PatientHeaderID <> @TargetPatientHeaderID 
            AND LabTestID NOT IN (
                SELECT LabTestID 
                FROM PatientLabResult 
                WHERE PatientHeaderID = @TargetPatientHeaderID 
                    AND IsDeleted = 0
            )
    )
GROUP BY 
    lt.ID,
    lt.TestDesciption,
    lt.MedicalClassDescription,
    lt.DisplayOrder,
    lt.ResultType,
    lt.DefaultTextResult,
    lt.MaleNormalMin,
    lt.MaleNormalMax,
    uom.Description,
    lt.Prefix,
    lt.Suffix
ORDER BY 
    lt.DisplayOrder,
    lt.MedicalClassDescription,
    lt.TestDesciption;

-- Summary statistics
PRINT '';
PRINT 'Summary:';
PRINT '==================================================================================';

SELECT 
    COUNT(DISTINCT lt.ID) AS TotalMissingLabTests,
    COUNT(DISTINCT CASE WHEN lt.ResultType = 1 THEN lt.ID END) AS NumericTests,
    COUNT(DISTINCT CASE WHEN lt.ResultType = 2 THEN lt.ID END) AS TextTests,
    COUNT(DISTINCT lt.MedicalClassDescription) AS DifferentMedicalClasses
FROM LabTest lt
WHERE lt.IsDeleted = 0
    AND lt.ID IN (
        SELECT DISTINCT LabTestID
        FROM PatientLabResult
        WHERE IsDeleted = 0 
            AND PatientHeaderID <> @TargetPatientHeaderID 
            AND LabTestID NOT IN (
                SELECT LabTestID 
                FROM PatientLabResult 
                WHERE PatientHeaderID = @TargetPatientHeaderID 
                    AND IsDeleted = 0
            )
    );

-- Show medical classes distribution
PRINT '';
PRINT 'Breakdown by Medical Class:';
PRINT '==================================================================================';

SELECT 
    lt.MedicalClassDescription AS MedicalClass,
    COUNT(DISTINCT lt.ID) AS TestCount
FROM LabTest lt
WHERE lt.IsDeleted = 0
    AND lt.ID IN (
        SELECT DISTINCT LabTestID
        FROM PatientLabResult
        WHERE IsDeleted = 0 
            AND PatientHeaderID <> @TargetPatientHeaderID 
            AND LabTestID NOT IN (
                SELECT LabTestID 
                FROM PatientLabResult 
                WHERE PatientHeaderID = @TargetPatientHeaderID 
                    AND IsDeleted = 0
            )
    )
GROUP BY lt.MedicalClassDescription
ORDER BY TestCount DESC, lt.MedicalClassDescription;

-- Show current lab tests for the target patient
PRINT '';
PRINT 'Current lab tests for this patient:';
PRINT '==================================================================================';

SELECT 
    LabTestID,
    LabTestDescription,
    MedicalClassDesc,
    DisplayOrder,
    Result,
    ResultDate
FROM PatientLabResult
WHERE PatientHeaderID = @TargetPatientHeaderID
    AND IsDeleted = 0
ORDER BY DisplayOrder, MedicalClassDesc;



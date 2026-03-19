-- Add LabTestID 136 (Antibiogram) to a Patient
-- This script adds the Antibiogram/Bacteriology test to a patient if they don't have it

-- ===== CONFIGURATION =====
DECLARE @AdmissionNumber VARCHAR(20) = '03.01186.08.24';  -- Change this to your patient
DECLARE @AntibiogramLabTestID INT = 136;
DECLARE @CreatedByUserID INT = 1;  -- Change to your user ID

-- ===== SCRIPT START =====
PRINT '====================================================================';
PRINT 'Adding LabTestID 136 (Antibiogram) to patient ' + @AdmissionNumber;
PRINT '====================================================================';

-- Check if patient exists
DECLARE @PatientHeaderID INT;

SELECT @PatientHeaderID = ID
FROM PatientLabResultsHeader
WHERE AdmissionNumber = @AdmissionNumber
    AND IsDeleted = 0;

IF @PatientHeaderID IS NULL
BEGIN
    PRINT '✗ ERROR: Patient header not found for admission number ' + @AdmissionNumber;
    PRINT 'Make sure the patient has a record in PatientLabResultsHeader table';
    RETURN;
END

PRINT '✓ Found Patient Header ID: ' + CAST(@PatientHeaderID AS VARCHAR(10));

-- Check if patient already has this lab test
IF EXISTS (
    SELECT 1 
    FROM PatientLabResult
    WHERE PatientHeaderID = @PatientHeaderID
        AND LabTestID = @AntibiogramLabTestID
        AND IsDeleted = 0
)
BEGIN
    PRINT '⚠ Patient already has LabTestID 136 (Antibiogram)';
    PRINT 'No action needed.';
    RETURN;
END

-- Insert the Antibiogram test
BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO PatientLabResult (
        PatientHeaderID,
        LabTestID,
        LabTestDescription,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        DisplayOrder,
        MedicalClass,
        MedicalClassDesc,
        DefaultTextResult,
        ErrorMax,
        ErrorMin,
        HighPanicIndex,
        LowPanicIndex,
        [Max],
        [Min],
        Ref_Range,
        UOM,
        UOMDescription,
        Prefix,
        Suffix
    )
    SELECT 
        @PatientHeaderID,           -- PatientHeaderID
        lt.ID,                      -- LabTestID
        lt.TestDesciption,          -- LabTestDescription
        0,                          -- IsDeleted
        @CreatedByUserID,           -- CreatedBy
        GETDATE(),                  -- CreatedDate
        lt.DisplayOrder,            -- DisplayOrder
        lt.MedicalClass,            -- MedicalClass
        lt.MedicalClassDescription, -- MedicalClassDesc
        lt.DefaultTextResult,       -- DefaultTextResult
        lt.ErrorRangeMax,           -- ErrorMax
        lt.ErrorRangeMin,           -- ErrorMin
        lt.HighPanicIndex,          -- HighPanicIndex
        lt.LowPanicIndex,           -- LowPanicIndex
        lt.MaleNormalMax,           -- Max
        lt.MaleNormalMin,           -- Min
        CASE 
            WHEN lt.MaleNormalMin IS NOT NULL AND lt.MaleNormalMax IS NOT NULL 
            THEN CONCAT(CAST(lt.MaleNormalMin AS VARCHAR), ' - ', CAST(lt.MaleNormalMax AS VARCHAR))
            ELSE NULL 
        END,                        -- Ref_Range
        lt.UOM,                     -- UOM
        uom.Description,            -- UOMDescription
        lt.Prefix,                  -- Prefix
        lt.Suffix                   -- Suffix
    FROM LabTest lt
    LEFT JOIN EMR.dbo.UnitOfMeasure uom ON lt.UOM = uom.ID
    WHERE lt.ID = @AntibiogramLabTestID
        AND lt.IsDeleted = 0;

    COMMIT TRANSACTION;

    PRINT '✓ Successfully added LabTestID 136 (Antibiogram) to patient';
    
    -- Show what was added
    SELECT 
        ID,
        LabTestID,
        LabTestDescription,
        MedicalClassDesc,
        CreatedDate
    FROM PatientLabResult
    WHERE PatientHeaderID = @PatientHeaderID
        AND LabTestID = @AntibiogramLabTestID
        AND IsDeleted = 0;

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '✗ ERROR: ' + ERROR_MESSAGE();
END CATCH

PRINT '====================================================================';






























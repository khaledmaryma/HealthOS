-- Simple Script to Add Missing Lab Tests to a Patient
-- This will add all lab tests that exist for other patients but not for the target patient

-- ===== CONFIGURATION =====
DECLARE @TargetPatientHeaderID INT = 108712;  -- Change this to your patient ID
DECLARE @CreatedByUserID INT = 1;              -- Change to the user ID creating these records

-- ===== SCRIPT START =====
DECLARE @LabTestID INT;
DECLARE @Counter INT = 0;
DECLARE @ErrorCount INT = 0;

PRINT 'Adding missing lab tests to Patient Header ID: ' + CAST(@TargetPatientHeaderID AS VARCHAR(10));
PRINT '================================================================';

DECLARE lab_test_cursor CURSOR FOR
    SELECT DISTINCT pr.LabTestID
    FROM PatientLabResult pr
    WHERE pr.IsDeleted = 0 
        AND pr.PatientHeaderID <> @TargetPatientHeaderID 
        AND pr.LabTestID NOT IN (
            SELECT LabTestID 
            FROM PatientLabResult 
            WHERE PatientHeaderID = @TargetPatientHeaderID 
                AND IsDeleted = 0
        )
    ORDER BY pr.LabTestID;

OPEN lab_test_cursor;
FETCH NEXT FROM lab_test_cursor INTO @LabTestID;

BEGIN TRANSACTION;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        -- Insert the lab test from LabTest master table
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
            Suffix,
            Result
        )
        SELECT 
            @TargetPatientHeaderID,     -- PatientHeaderID
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
            lt.Suffix,                  -- Suffix
            CASE 
                WHEN lt.DefaultTextResult IS NOT NULL AND lt.DefaultTextResult <> '' 
                THEN lt.DefaultTextResult 
                ELSE NULL 
            END                         -- Result (use default text if available)
        FROM LabTest lt
        LEFT JOIN EMR.dbo.UnitOfMeasure uom ON lt.UOM = uom.ID
        WHERE lt.ID = @LabTestID
            AND lt.IsDeleted = 0;
        
        SET @Counter = @Counter + 1;
        PRINT 'Added LabTestID ' + CAST(@LabTestID AS VARCHAR(10));
    END TRY
    BEGIN CATCH
        SET @ErrorCount = @ErrorCount + 1;
        PRINT 'ERROR adding LabTestID ' + CAST(@LabTestID AS VARCHAR(10)) + ': ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM lab_test_cursor INTO @LabTestID;
END

CLOSE lab_test_cursor;
DEALLOCATE lab_test_cursor;

COMMIT TRANSACTION;

PRINT '================================================================';
PRINT 'Summary:';
PRINT '  Successfully added: ' + CAST(@Counter AS VARCHAR(10)) + ' lab tests';
PRINT '  Errors: ' + CAST(@ErrorCount AS VARCHAR(10));
PRINT '================================================================';

-- Show what was added
SELECT 
    LabTestID,
    LabTestDescription,
    MedicalClassDesc,
    DisplayOrder,
    CreatedDate
FROM PatientLabResult
WHERE PatientHeaderID = @TargetPatientHeaderID
    AND IsDeleted = 0
    AND CAST(CreatedDate AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY DisplayOrder, MedicalClassDesc;



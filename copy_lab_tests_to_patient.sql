-- SQL Script to Loop Through Lab Tests Missing for Patient 108712
-- This script finds lab tests that exist for other patients but not for patient 108712
-- and can be used to copy them or perform other operations

DECLARE @LabTestID INT;
DECLARE @LabTestDescription NVARCHAR(255);
DECLARE @PatientHeaderID INT = 108712;
DECLARE @Counter INT = 0;
DECLARE @TotalCount INT = 0;

-- Cursor to loop through the distinct LabTestIDs
DECLARE lab_test_cursor CURSOR FOR
    SELECT DISTINCT 
        pr.LabTestID,
        pr.LabTestDescription
    FROM PatientLabResult pr
    WHERE pr.IsDeleted = 0 
        AND pr.PatientHeaderID <> @PatientHeaderID 
        AND pr.LabTestID NOT IN (
            SELECT LabTestID 
            FROM PatientLabResult 
            WHERE PatientHeaderID = @PatientHeaderID 
                AND IsDeleted = 0
        )
    ORDER BY pr.LabTestID;

-- Get total count for progress tracking
SELECT @TotalCount = COUNT(DISTINCT pr.LabTestID)
FROM PatientLabResult pr
WHERE pr.IsDeleted = 0 
    AND pr.PatientHeaderID <> @PatientHeaderID 
    AND pr.LabTestID NOT IN (
        SELECT LabTestID 
        FROM PatientLabResult 
        WHERE PatientHeaderID = @PatientHeaderID 
            AND IsDeleted = 0
    );

PRINT '====================================================';
PRINT 'Starting to process lab tests missing for Patient ' + CAST(@PatientHeaderID AS VARCHAR(10));
PRINT 'Total lab tests to process: ' + CAST(@TotalCount AS VARCHAR(10));
PRINT '====================================================';
PRINT '';

-- Open the cursor
OPEN lab_test_cursor;

-- Fetch the first row
FETCH NEXT FROM lab_test_cursor INTO @LabTestID, @LabTestDescription;

-- Loop through all rows
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Counter = @Counter + 1;
    
    PRINT 'Processing ' + CAST(@Counter AS VARCHAR(10)) + ' of ' + CAST(@TotalCount AS VARCHAR(10));
    PRINT 'LabTestID: ' + CAST(@LabTestID AS VARCHAR(10)) + ' - ' + ISNULL(@LabTestDescription, 'N/A');
    
    -- ======================================================================
    -- INSERT YOUR PROCESSING LOGIC HERE
    -- ======================================================================
    
    -- OPTION 1: Just display the lab test (current behavior)
    -- Already displayed above
    
    -- OPTION 2: Insert a new lab result record for this patient
    -- Uncomment the following block to insert records
    /*
    BEGIN TRY
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
            -- Add other required fields from the LabTest table
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
            1,                          -- CreatedBy (adjust as needed)
            GETDATE(),                  -- CreatedDate
            lt.DisplayOrder,            -- DisplayOrder
            lt.MedicalClass,            -- MedicalClass
            lt.MedicalClassDescription, -- MedicalClassDesc
            lt.DefaultTextResult,       -- DefaultTextResult
            lt.ErrorRangeMax,           -- ErrorMax
            lt.ErrorRangeMin,           -- ErrorMin
            lt.HighPanicIndex,          -- HighPanicIndex
            lt.LowPanicIndex,           -- LowPanicIndex
            lt.MaleNormalMax,           -- Max (you can choose male, female, or default)
            lt.MaleNormalMin,           -- Min
            CONCAT(CAST(lt.MaleNormalMin AS VARCHAR), ' - ', CAST(lt.MaleNormalMax AS VARCHAR)), -- Ref_Range
            lt.UOM,                     -- UOM
            uom.Description,            -- UOMDescription
            lt.Prefix,                  -- Prefix
            lt.Suffix                   -- Suffix
        FROM LabTest lt
        LEFT JOIN EMR.dbo.UnitOfMeasure uom ON lt.UOM = uom.ID
        WHERE lt.ID = @LabTestID
            AND lt.IsDeleted = 0;
        
        PRINT '  ✓ Successfully added lab test to patient';
    END TRY
    BEGIN CATCH
        PRINT '  ✗ Error adding lab test: ' + ERROR_MESSAGE();
    END CATCH
    */
    
    -- OPTION 3: Copy from a template patient
    -- Uncomment to copy from another patient's result
    /*
    DECLARE @TemplatePatientHeaderID INT = 12345; -- Change to a template patient ID
    
    BEGIN TRY
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
            @PatientHeaderID,           -- PatientHeaderID (the target patient)
            LabTestID,
            LabTestDescription,
            0,                          -- IsDeleted
            1,                          -- CreatedBy (adjust as needed)
            GETDATE(),                  -- CreatedDate
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
        FROM PatientLabResult
        WHERE PatientHeaderID = @TemplatePatientHeaderID
            AND LabTestID = @LabTestID
            AND IsDeleted = 0
        ORDER BY DisplayOrder
        OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY; -- Get first matching record
        
        PRINT '  ✓ Successfully copied lab test from template patient';
    END TRY
    BEGIN CATCH
        PRINT '  ✗ Error copying lab test: ' + ERROR_MESSAGE();
    END CATCH
    */
    
    -- ======================================================================
    
    PRINT '';
    
    -- Fetch the next row
    FETCH NEXT FROM lab_test_cursor INTO @LabTestID, @LabTestDescription;
END

-- Close and deallocate the cursor
CLOSE lab_test_cursor;
DEALLOCATE lab_test_cursor;

PRINT '====================================================';
PRINT 'Processing completed!';
PRINT 'Total lab tests processed: ' + CAST(@Counter AS VARCHAR(10));
PRINT '====================================================';

-- Optional: Display summary of what was processed
SELECT 
    @Counter AS [Total Processed],
    @TotalCount AS [Total Found],
    @PatientHeaderID AS [Target Patient Header ID];



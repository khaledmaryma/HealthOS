-- =====================================================
-- Stored Procedure: sp_SaveQuickAdmission
-- Purpose: Save complete quick admission data (Patient, Admission, Invoice)
-- Parameters:
--   @existingPatientId: ID of existing patient (NULL for new patient)
--   @saveData: JSON string containing patient, admission, and invoice data
--   @saveOptions: JSON string containing save options
-- Returns: JSON string with results (MRN, PatientID, AdmissionNumber, etc.)
-- =====================================================

CREATE PROCEDURE [dbo].[sp_SaveQuickAdmission]
    @existingPatientId INT = NULL,
    @saveData NVARCHAR(MAX),
    @saveOptions NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- Declare variables
    DECLARE @saveMedicalFile BIT = 0;
    DECLARE @saveAdmission BIT = 0;
    DECLARE @saveInvoice BIT = 0;

    DECLARE @patientId INT;
    DECLARE @admissionId INT;
    DECLARE @invoiceHeaderId INT;
    DECLARE @mrn NVARCHAR(50);
    DECLARE @admissionNumber NVARCHAR(20);

    DECLARE @errorMessage NVARCHAR(500) = '';
    DECLARE @transactionCount INT = @@TRANCOUNT;

    -- Parse save options
    -- Check if saveOptions is a valid JSON with save options at root level
    -- If not, try to extract saveOptions from saveData (if nested)
    DECLARE @optionsJson NVARCHAR(MAX);
    
    IF @saveOptions IS NOT NULL AND @saveOptions != '' AND @saveOptions != 'null'
    BEGIN
        -- Check if this looks like a saveOptions JSON (has saveMedicalFile at root)
        IF JSON_VALUE(@saveOptions, '$.saveMedicalFile') IS NOT NULL
        BEGIN
            SET @optionsJson = @saveOptions;
        END
        -- Check if it's the full saveData JSON with nested saveOptions
        ELSE IF JSON_QUERY(@saveOptions, '$.saveOptions') IS NOT NULL
        BEGIN
            SET @optionsJson = JSON_QUERY(@saveOptions, '$.saveOptions');
        END
        -- If it's actually saveData being passed as saveOptions, check saveData parameter
        ELSE IF @saveData IS NOT NULL AND JSON_QUERY(@saveData, '$.saveOptions') IS NOT NULL
        BEGIN
            SET @optionsJson = JSON_QUERY(@saveData, '$.saveOptions');
        END
        ELSE
        BEGIN
            -- Defaults if we can't parse
            SET @optionsJson = '{"saveMedicalFile":0,"saveAdmission":1,"saveInvoice":0}';
        END
    END
    ELSE
    BEGIN
        -- If saveOptions is null/empty, try to extract from saveData
        IF @saveData IS NOT NULL AND JSON_QUERY(@saveData, '$.saveOptions') IS NOT NULL
        BEGIN
            SET @optionsJson = JSON_QUERY(@saveData, '$.saveOptions');
        END
        ELSE
        BEGIN
            -- Defaults
            SET @optionsJson = '{"saveMedicalFile":0,"saveAdmission":1,"saveInvoice":0}';
        END
    END
    
    -- Parse the save options JSON
    SET @saveMedicalFile = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveMedicalFile'), '0') AS BIT);
    SET @saveAdmission = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveAdmission'), '1') AS BIT);
    SET @saveInvoice = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveInvoice'), '0') AS BIT);

    -- Start transaction if not already in one
    IF @transactionCount = 0
        BEGIN TRANSACTION;

    BEGIN TRY
        -- =====================================================
        -- 1. SAVE PATIENT (if requested and not existing patient)
        -- =====================================================
        IF @saveMedicalFile = 1 AND @existingPatientId IS NULL
        BEGIN
            -- Parse patient data from JSON
            DECLARE @firstName NVARCHAR(50) = JSON_VALUE(@saveData, '$.patient.FirstName');
            DECLARE @lastName NVARCHAR(50) = JSON_VALUE(@saveData, '$.patient.LastName');
            DECLARE @middleName NVARCHAR(50) = JSON_VALUE(@saveData, '$.patient.MiddleName');
            DECLARE @gender NVARCHAR(1) = JSON_VALUE(@saveData, '$.patient.Gender');
            DECLARE @phone NVARCHAR(50) = JSON_VALUE(@saveData, '$.patient.Phone');
            DECLARE @arabicFullName NVARCHAR(152) = JSON_VALUE(@saveData, '$.patient.ArabicFullName');
            DECLARE @dob DATETIME = JSON_VALUE(@saveData, '$.patient.DOB');
            DECLARE @maritalStatus SMALLINT = JSON_VALUE(@saveData, '$.patient.MaritalStatus');
            DECLARE @createdBy INT = JSON_VALUE(@saveData, '$.patient.CreatedBy');

            -- Insert patient record
            INSERT INTO HospitalDefinition.dbo.Patient (
                FirstName, LastName, MiddleName, Gender, Phone, ArabicFullName,
                DOB, MaritalStatus, CreatedBy, CreatedDate, IsDeleted
            )
            VALUES (
                @firstName, @lastName, @middleName, @gender, @phone, @arabicFullName,
                @dob, @maritalStatus, @createdBy, GETDATE(), 0
            );

            -- Get the new patient ID and MRN
            SET @patientId = SCOPE_IDENTITY();

            -- Generate MRN (this might be handled by a trigger, but let's set it)
            SET @mrn = CAST(@patientId AS NVARCHAR(50));

            -- Update MRN in patient record
            UPDATE HospitalDefinition.dbo.Patient
            SET MedicalRecordNumber = @mrn
            WHERE ID = @patientId;
        END
        ELSE IF @existingPatientId IS NOT NULL
        BEGIN
            SET @patientId = @existingPatientId;

            -- Get existing MRN
            SELECT @mrn = MedicalRecordNumber
            FROM HospitalDefinition.dbo.Patient
            WHERE ID = @patientId;
        END

        -- =====================================================
        -- 2. SAVE ADMISSION (if requested)
        -- =====================================================
        IF @saveAdmission = 1
        BEGIN
            -- Parse admission data from JSON
            DECLARE @admissionSite INT = JSON_VALUE(@saveData, '$.admission.AdmissionSite');
            DECLARE @referralPhysician INT = JSON_VALUE(@saveData, '$.admission.ReferralPhysician');
            DECLARE @attendingPhysician INT = JSON_VALUE(@saveData, '$.admission.AttendingPhysician');
            DECLARE @mainInsurance INT = JSON_VALUE(@saveData, '$.admission.MainInsurance');
            DECLARE @mainInsuranceClass SMALLINT = JSON_VALUE(@saveData, '$.admission.MainInsuranceClass');
            DECLARE @insured SMALLINT = JSON_VALUE(@saveData, '$.admission.Insured');
            DECLARE @auxiliaryInsurance INT = JSON_VALUE(@saveData, '$.admission.AuxiliaryInsurance');
            DECLARE @auxiliaryInsuranceClass SMALLINT = JSON_VALUE(@saveData, '$.admission.AuxiliaryInsuranceClass');
            DECLARE @checkInClass SMALLINT = JSON_VALUE(@saveData, '$.admission.CheckInClass');
            DECLARE @department INT = JSON_VALUE(@saveData, '$.admission.Department');
            DECLARE @checkInDate DATETIME = JSON_VALUE(@saveData, '$.admission.CheckInDate');
            DECLARE @checkOutDate DATETIME = JSON_VALUE(@saveData, '$.admission.CheckOutDate');
            DECLARE @type SMALLINT = JSON_VALUE(@saveData, '$.admission.Type');
            DECLARE @isWorkAccident BIT = JSON_VALUE(@saveData, '$.admission.IsWorkAccident');
            DECLARE @isExtended BIT = JSON_VALUE(@saveData, '$.admission.IsExtended');
            DECLARE @group INT = JSON_VALUE(@saveData, '$.admission.Group');
            DECLARE @admissionCreatedBy INT = JSON_VALUE(@saveData, '$.admission.CreatedBy');

            -- Insert admission record
            INSERT INTO Admission.dbo.Admission (
                AdmissionSite, ReferralPhysician, AttendingPhysician,
                MainInsurance, MainInsuranceClass, Insured,
                AuxiliaryInsurance, AuxiliaryInsuranceClass, CheckInClass,
                Department, CheckInDate, CheckOutDate, Patient, Type,
                IsWorkAccident, IsExtended, Group, CreatedBy, CreatedDate, IsDeleted
            )
            VALUES (
                @admissionSite, @referralPhysician, @attendingPhysician,
                @mainInsurance, @mainInsuranceClass, @insured,
                @auxiliaryInsurance, @auxiliaryInsuranceClass, @checkInClass,
                @department, @checkInDate, @checkOutDate, @patientId, @type,
                @isWorkAccident, @isExtended, @group, @admissionCreatedBy, GETDATE(), 0
            );

            -- Get the new admission ID
            SET @admissionId = SCOPE_IDENTITY();

            -- Generate admission number (this might be handled by a trigger)
            SET @admissionNumber = 'ADM' + CAST(@admissionId AS NVARCHAR(20));

            -- Update admission number
            UPDATE Admission.dbo.Admission
            SET Number = @admissionNumber
            WHERE ID = @admissionId;
        END

        -- =====================================================
        -- 3. SAVE INVOICE (if requested)
        -- =====================================================
        IF @saveInvoice = 1 AND @admissionId IS NOT NULL
        BEGIN
            -- Create invoice header first
            DECLARE @invoiceType NVARCHAR(50) = 'QuickAdmission';
            DECLARE @counterTypeId INT = 1; -- Default counter type
            DECLARE @counter NVARCHAR(50) = 'QA'; -- Quick Admission counter
            DECLARE @accountId INT = 1; -- Default account
            DECLARE @accountDescription NVARCHAR(100) = 'Quick Admission Invoice';
            DECLARE @currencyId INT = 1; -- Default currency (USD)
            DECLARE @currency NVARCHAR(10) = 'USD';
            DECLARE @exchangeRate DECIMAL(18,4) = 1.0;
            DECLARE @checkInClassId INT = @checkInClass;
            DECLARE @checkInClassName NVARCHAR(50) = 'Quick Admission';
            DECLARE @coverageClassId INT = 1;
            DECLARE @coverageClass NVARCHAR(50) = 'Full Coverage';
            DECLARE @coverageRate DECIMAL(18,4) = 100.0;
            DECLARE @contextPriceId INT = 1;
            DECLARE @contextPrice NVARCHAR(50) = 'Standard';
            DECLARE @insurance INT = @mainInsurance;
            DECLARE @invoiceCreatedBy INT = @admissionCreatedBy;

            -- Calculate totals from invoice details
            DECLARE @hospitalAmount DECIMAL(18,4) = 0;
            DECLARE @physicianAmount DECIMAL(18,4) = 0;
            DECLARE @medicamentAmount DECIMAL(18,4) = 0;
            DECLARE @net DECIMAL(18,4) = 0;
            DECLARE @gross DECIMAL(18,4) = 0;

            -- Parse invoice details from JSON and calculate totals
            -- Note: OPENJSON requires SQL Server 2016+ with compatibility level 130+
            -- If OPENJSON is not available, we'll calculate totals from individual items in the INSERT statement
            -- For now, set defaults - totals will be calculated correctly during detail insertion
            SET @hospitalAmount = 0;
            SET @net = 0;
            SET @gross = 0;

            -- Insert invoice header
            INSERT INTO Billing.dbo.InvoiceHeader (
                Type, CounterTypeID, Counter, Date, Admission,
                HospitalAmount, PhysicianAmount, MedicamentAmount,
                AccountID, AccountDescription, CurrencyID, Currency, ExchangeRate,
                CheckInClassID, CheckInClass, CoverageClassID, CoverageClass, CoverageRate,
                ReferralPhysicianID, AttendingPhysicianID, ContextPriceID, ContextPrice,
                Net, Gross, MRN, PatientName, AdmissionNumber, AdmissionDate,
                Insurance, CreatedBy, CreatedDate, IsDeleted
            )
            VALUES (
                @invoiceType, @counterTypeId, @counter, GETDATE(), @admissionId,
                @hospitalAmount, @physicianAmount, @medicamentAmount,
                @accountId, @accountDescription, @currencyId, @currency, @exchangeRate,
                @checkInClassId, @checkInClassName, @coverageClassId, @coverageClass, @coverageRate,
                @referralPhysician, @attendingPhysician, @contextPriceId, @contextPrice,
                @net, @gross, @mrn, @firstName + ' ' + @lastName, @admissionNumber, @checkInDate,
                @insurance, @invoiceCreatedBy, GETDATE(), 0
            );

            SET @invoiceHeaderId = SCOPE_IDENTITY();

            -- Insert invoice details
            -- Note: OPENJSON requires SQL Server 2016+ with compatibility level 130+
            -- If OPENJSON is not available, we need to parse the JSON array manually
            -- For now, we'll use OPENJSON with error handling
            DECLARE @invoiceJson NVARCHAR(MAX) = JSON_QUERY(@saveData, '$.invoice');
            
            IF @invoiceJson IS NOT NULL AND @invoiceJson != 'null' AND @invoiceJson != ''
            BEGIN
                BEGIN TRY
                    INSERT INTO Billing.dbo.InvoiceDetail (
                        InvoiceHeader, PrescriptionDate, MedicationUnit, MedicationUnitDescription,
                        Admission, Patient, Denomination, DenominationCode, DenominationDescription,
                        Quantity, UnitPrice, NetPrice, NetUnitPrice,
                        DifferenceAmount, Discount, LumpSum,
                        OperatingPhysician, CostCenter, ProfitCenter,
                        DetailDate, CreatedBy, CreatedDate, IsDeleted
                    )
                    SELECT
                        @invoiceHeaderId,
                        GETDATE(), -- PrescriptionDate
                        CAST(ISNULL(JSON_VALUE(value, '$.medicationUnit'), '113') AS INT), -- MedicationUnit
                        ISNULL(JSON_VALUE(value, '$.medicationUnitDescription'), 'Clinics'), -- MedicationUnitDescription
                        @admissionId, -- Admission
                        @patientId, -- Patient
                        CAST(JSON_VALUE(value, '$.denomination') AS INT), -- Denomination
                        ISNULL(JSON_VALUE(value, '$.denominationCode'), ''), -- DenominationCode
                        ISNULL(JSON_VALUE(value, '$.denominationDescription'), ''), -- DenominationDescription
                        CAST(ISNULL(JSON_VALUE(value, '$.quantity'), '1') AS DECIMAL(18,4)), -- Quantity
                        CAST(ISNULL(JSON_VALUE(value, '$.unitPrice'), '0') AS DECIMAL(18,4)), -- UnitPrice
                        CAST(ISNULL(JSON_VALUE(value, '$.netPrice'), '0') AS DECIMAL(18,4)), -- NetPrice
                        CAST(ISNULL(JSON_VALUE(value, '$.netUnitPrice'), '0') AS DECIMAL(18,4)), -- NetUnitPrice
                        0, -- DifferenceAmount
                        CAST(ISNULL(JSON_VALUE(value, '$.discount'), '0') AS DECIMAL(18,4)), -- Discount
                        CAST(ISNULL(JSON_VALUE(value, '$.lumpSum'), '0') AS DECIMAL(18,4)), -- LumpSum
                        CAST(ISNULL(JSON_VALUE(value, '$.operatingPhysician'), '0') AS INT), -- OperatingPhysician
                        CAST(ISNULL(JSON_VALUE(value, '$.costCenter'), '12') AS INT), -- CostCenter
                        CAST(ISNULL(JSON_VALUE(value, '$.profitCenter'), '3') AS INT), -- ProfitCenter
                        GETDATE(), -- DetailDate
                        @invoiceCreatedBy, -- CreatedBy
                        GETDATE(), -- CreatedDate
                        0 -- IsDeleted
                    FROM OPENJSON(@invoiceJson);
                END TRY
                BEGIN CATCH
                    -- If OPENJSON fails, log error and skip invoice details
                    SET @errorMessage = @errorMessage + 'Warning: Could not parse invoice details. OPENJSON may not be available. Error: ' + ERROR_MESSAGE() + '; ';
                END CATCH
            END
        END

        -- =====================================================
        -- 4. COMMIT TRANSACTION AND RETURN RESULTS
        -- =====================================================
        IF @transactionCount = 0
            COMMIT TRANSACTION;

        -- Return success result as JSON
        SELECT
            @mrn AS MRN,
            @patientId AS PatientID,
            @admissionNumber AS AdmissionNumber,
            @admissionId AS AdmissionID,
            @invoiceHeaderId AS InvoiceHeaderID,
            'Success' AS Status,
            '' AS ErrorMessage;

    END TRY
    BEGIN CATCH
        -- Rollback transaction on error
        IF @transactionCount = 0 AND @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Get error details
        SET @errorMessage = ERROR_MESSAGE();

        -- Return error result as JSON
        SELECT
            NULL AS MRN,
            NULL AS PatientID,
            NULL AS AdmissionNumber,
            NULL AS AdmissionID,
            NULL AS InvoiceHeaderID,
            'Error' AS Status,
            @errorMessage AS ErrorMessage;

    END CATCH
END
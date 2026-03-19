-- =====================================================
-- DIAGNOSTIC VERSION: sp_SaveQuickAdmission
-- This version adds timing and logging to identify where execution hangs
-- =====================================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_SaveQuickAdmission]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_SaveQuickAdmission];
END
GO

CREATE PROCEDURE [dbo].[sp_SaveQuickAdmission]
    @existingPatientId INT = NULL,
    @saveData NVARCHAR(MAX),
    @saveOptions NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- Timing variables
    DECLARE @startTime DATETIME = GETDATE();
    DECLARE @stepTime DATETIME;
    DECLARE @elapsedSeconds INT;

    -- Log start
    PRINT '=== SP STARTED: ' + CONVERT(VARCHAR(23), @startTime, 121);

    -- Declare variables
    DECLARE @saveMedicalFile BIT = 0;
    DECLARE @saveAdmission BIT = 0;
    DECLARE @saveInvoice BIT = 0;

    DECLARE @patientId INT;
    DECLARE @admissionId INT;
    DECLARE @invoiceHeaderId INT;
    DECLARE @mrn NVARCHAR(50) = '';
    DECLARE @admissionNumber NVARCHAR(20) = '';

    DECLARE @errorMessage NVARCHAR(MAX) = '';
    DECLARE @transactionCount INT = @@TRANCOUNT;

    -- Variables for patient data
    DECLARE @firstName NVARCHAR(50);
    DECLARE @lastName NVARCHAR(50);
    DECLARE @middleName NVARCHAR(50);
    DECLARE @gender NVARCHAR(1);
    DECLARE @phone NVARCHAR(50);
    DECLARE @arabicFullName NVARCHAR(152);
    DECLARE @dob DATETIME;
    DECLARE @maritalStatus SMALLINT;
    DECLARE @createdBy INT;

    -- Variables for admission data
    DECLARE @admissionSite INT;
    DECLARE @referralPhysician INT;
    DECLARE @attendingPhysician INT;
    DECLARE @mainInsurance INT;
    DECLARE @mainInsuranceClass SMALLINT;
    DECLARE @insured SMALLINT;
    DECLARE @auxiliaryInsurance INT;
    DECLARE @auxiliaryInsuranceClass SMALLINT;
    DECLARE @checkInClass SMALLINT;
    DECLARE @department NVARCHAR(50);
    DECLARE @checkInDate DATETIME;
    DECLARE @checkOutDate DATETIME;
    DECLARE @type SMALLINT;
    DECLARE @isWorkAccident BIT;
    DECLARE @isExtended BIT;
    DECLARE @group INT;
    DECLARE @admissionCreatedBy INT;

    -- Start transaction if not already in one
    IF @transactionCount = 0
    BEGIN
        PRINT '=== STARTING TRANSACTION';
        BEGIN TRANSACTION;
    END

    BEGIN TRY
        -- Parse save options
        SET @stepTime = GETDATE();
        PRINT '=== STEP 1: Parsing save options...';
        
        DECLARE @optionsJson NVARCHAR(MAX);
        
        IF @saveOptions IS NOT NULL AND @saveOptions != '' AND @saveOptions != 'null'
        BEGIN
            SET @optionsJson = JSON_QUERY(@saveOptions, '$.saveOptions');
            
            IF @optionsJson IS NULL OR @optionsJson = 'null' OR @optionsJson = ''
            BEGIN
                IF JSON_VALUE(@saveOptions, '$.saveMedicalFile') IS NOT NULL
                BEGIN
                    SET @optionsJson = @saveOptions;
                END
                ELSE IF @saveData IS NOT NULL
                BEGIN
                    SET @optionsJson = JSON_QUERY(@saveData, '$.saveOptions');
                END
            END
        END
        ELSE
        BEGIN
            IF @saveData IS NOT NULL
            BEGIN
                SET @optionsJson = JSON_QUERY(@saveData, '$.saveOptions');
            END
        END
        
        IF @optionsJson IS NULL OR @optionsJson = '' OR @optionsJson = 'null'
        BEGIN
            SET @optionsJson = '{"saveMedicalFile":1,"saveAdmission":1,"saveInvoice":1}';
        END
        
        SET @saveMedicalFile = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveMedicalFile'), '1') AS BIT);
        SET @saveAdmission = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveAdmission'), '1') AS BIT);
        SET @saveInvoice = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveInvoice'), '0') AS BIT);

        SET @elapsedSeconds = DATEDIFF(SECOND, @stepTime, GETDATE());
        PRINT '=== STEP 1 COMPLETE: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';

        -- =====================================================
        -- 1. SAVE PATIENT
        -- =====================================================
        IF @saveMedicalFile = 1 AND @existingPatientId IS NULL AND @saveData IS NOT NULL
        BEGIN
            SET @stepTime = GETDATE();
            PRINT '=== STEP 2: Saving patient...';
            
            SET @firstName = JSON_VALUE(@saveData, '$.patient.FirstName');
            SET @lastName = JSON_VALUE(@saveData, '$.patient.LastName');
            SET @middleName = JSON_VALUE(@saveData, '$.patient.MiddleName');
            SET @gender = JSON_VALUE(@saveData, '$.patient.Gender');
            SET @phone = JSON_VALUE(@saveData, '$.patient.Phone');
            SET @arabicFullName = JSON_VALUE(@saveData, '$.patient.ArabicFullName');
            
            DECLARE @dobString NVARCHAR(50) = JSON_VALUE(@saveData, '$.patient.DOB');
            IF @dobString IS NOT NULL AND @dobString != ''
            BEGIN
                SET @dob = TRY_CAST(@dobString AS DATETIME);
            END
            
            SET @maritalStatus = CAST(ISNULL(JSON_VALUE(@saveData, '$.patient.MaritalStatus'), '0') AS SMALLINT);
            SET @createdBy = CAST(ISNULL(JSON_VALUE(@saveData, '$.patient.CreatedBy'), '338') AS INT);

            PRINT '=== Inserting into HospitalDefinition.dbo.Patient...';
            INSERT INTO HospitalDefinition.dbo.Patient (
                FirstName, LastName, MiddleName, Gender, Phone, 
                ArabicFullName, DOB, MaritalStatus, CreatedBy, CreatedDate, IsDeleted
            )
            VALUES (
                @firstName, @lastName, @middleName, @gender, @phone,
                @arabicFullName, @dob, @maritalStatus, @createdBy, GETDATE(), 0
            );

            SET @patientId = SCOPE_IDENTITY();
            PRINT '=== Patient ID: ' + CAST(@patientId AS VARCHAR(10));

            SELECT @mrn = ISNULL(MedicalRecordNumber, CAST(@patientId AS NVARCHAR(50)))
            FROM HospitalDefinition.dbo.Patient
            WHERE ID = @patientId;

            IF @mrn IS NULL OR @mrn = ''
            BEGIN
                SET @mrn = CAST(@patientId AS NVARCHAR(50));
                UPDATE HospitalDefinition.dbo.Patient
                SET MedicalRecordNumber = @mrn
                WHERE ID = @patientId;
            END

            SET @elapsedSeconds = DATEDIFF(SECOND, @stepTime, GETDATE());
            PRINT '=== STEP 2 COMPLETE: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';
        END
        ELSE IF @existingPatientId IS NOT NULL
        BEGIN
            SET @stepTime = GETDATE();
            PRINT '=== STEP 2: Using existing patient ID: ' + CAST(@existingPatientId AS VARCHAR(10));
            
            SET @patientId = @existingPatientId;
            SELECT 
                @mrn = ISNULL(MedicalRecordNumber, CAST(@patientId AS NVARCHAR(50))),
                @firstName = FirstName,
                @lastName = LastName,
                @middleName = MiddleName
            FROM HospitalDefinition.dbo.Patient
            WHERE ID = @patientId AND (IsDeleted = 0 OR IsDeleted IS NULL);

            SET @elapsedSeconds = DATEDIFF(SECOND, @stepTime, GETDATE());
            PRINT '=== STEP 2 COMPLETE: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';
        END

        -- =====================================================
        -- 2. SAVE ADMISSION
        -- =====================================================
        IF @saveAdmission = 1 AND @patientId IS NOT NULL AND @saveData IS NOT NULL
        BEGIN
            SET @stepTime = GETDATE();
            PRINT '=== STEP 3: Saving admission...';
            
            SET @admissionSite = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.AdmissionSite'), '0') AS INT);
            SET @referralPhysician = CAST(JSON_VALUE(@saveData, '$.admission.ReferralPhysician') AS INT);
            SET @attendingPhysician = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.AttendingPhysician'), CAST(JSON_VALUE(@saveData, '$.admission.ReferralPhysician') AS INT)) AS INT);
            SET @mainInsurance = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.MainInsurance'), '0') AS INT);
            SET @mainInsuranceClass = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.MainInsuranceClass'), '0') AS SMALLINT);
            SET @insured = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.Insured'), '0') AS SMALLINT);
            SET @auxiliaryInsurance = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.AuxiliaryInsurance'), '0') AS INT);
            SET @auxiliaryInsuranceClass = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.AuxiliaryInsuranceClass'), '0') AS SMALLINT);
            SET @checkInClass = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.CheckInClass'), '0') AS SMALLINT);
            SET @department = JSON_VALUE(@saveData, '$.admission.Department');
            
            DECLARE @checkInDateString NVARCHAR(50) = JSON_VALUE(@saveData, '$.admission.CheckInDate');
            IF @checkInDateString IS NOT NULL AND @checkInDateString != ''
            BEGIN
                SET @checkInDate = TRY_CAST(@checkInDateString AS DATETIME);
                IF @checkInDate IS NULL
                    SET @checkInDate = GETDATE();
            END
            ELSE
                SET @checkInDate = GETDATE();
            
            DECLARE @checkOutDateString NVARCHAR(50) = JSON_VALUE(@saveData, '$.admission.CheckOutDate');
            IF @checkOutDateString IS NOT NULL AND @checkOutDateString != ''
            BEGIN
                SET @checkOutDate = TRY_CAST(@checkOutDateString AS DATETIME);
            END
            
            SET @type = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.Type'), '0') AS SMALLINT);
            SET @isWorkAccident = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.IsWorkAccident'), '0') AS BIT);
            SET @isExtended = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.IsExtended'), '0') AS BIT);
            SET @group = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.Group'), '0') AS INT);
            SET @admissionCreatedBy = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.CreatedBy'), '338') AS INT);

            IF @referralPhysician IS NULL OR @referralPhysician = 0
            BEGIN
                RAISERROR('Referral Physician is required', 16, 1);
            END

            IF @checkInDate IS NULL
            BEGIN
                RAISERROR('Check-in Date is required', 16, 1);
            END

            -- Generate admission number - THIS IS LIKELY WHERE IT HANGS!
            PRINT '=== CALLING GenerateAdmissionNumber_V1 - THIS MAY BLOCK...';
            SET @stepTime = GETDATE();
            
            EXEC [dbo].[GenerateAdmissionNumber_V1] @checkInDate, @type, @admissionNumber OUTPUT;
            
            SET @elapsedSeconds = DATEDIFF(SECOND, @stepTime, GETDATE());
            PRINT '=== GenerateAdmissionNumber_V1 COMPLETE: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';
            PRINT '=== Admission Number: ' + ISNULL(@admissionNumber, 'NULL');

            -- Insert admission record
            PRINT '=== Inserting into dbo.Admission...';
            SET @stepTime = GETDATE();
            
            INSERT INTO dbo.Admission (
                Number, AdmissionSite, ReferralPhysician, AttendingPhysician,
                MainInsurance, MainInsuranceClass, Insured,
                AuxiliaryInsurance, AuxiliaryInsuranceClass, CheckInClass,
                Department, CheckInDate, CheckOutDate, Patient, Type,
                IsWorkAccident, IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
            )
            VALUES (
                @admissionNumber, 
                NULLIF(@admissionSite, 0), @referralPhysician, NULLIF(@attendingPhysician, 0),
                NULLIF(@mainInsurance, 0), NULLIF(@mainInsuranceClass, 0), NULLIF(@insured, 0),
                NULLIF(@auxiliaryInsurance, 0), NULLIF(@auxiliaryInsuranceClass, 0), NULLIF(@checkInClass, 0),
                @department, @checkInDate, @checkOutDate, @patientId, NULLIF(@type, 0),
                @isWorkAccident, @isExtended, NULLIF(@group, 0), @admissionCreatedBy, GETDATE(), 0
            );

            SET @admissionId = SCOPE_IDENTITY();
            PRINT '=== Admission ID: ' + CAST(@admissionId AS VARCHAR(10));

            SET @elapsedSeconds = DATEDIFF(SECOND, @stepTime, GETDATE());
            PRINT '=== STEP 3 COMPLETE: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';
        END

        -- =====================================================
        -- 3. SAVE INVOICE
        -- =====================================================
        IF @saveInvoice = 1 AND @admissionId IS NOT NULL AND @saveData IS NOT NULL
        BEGIN
            SET @stepTime = GETDATE();
            PRINT '=== STEP 4: Saving invoice...';
            
            DECLARE @invoiceJson NVARCHAR(MAX) = JSON_QUERY(@saveData, '$.invoice');
            
            IF @invoiceJson IS NOT NULL AND @invoiceJson != 'null' AND @invoiceJson != '' AND @invoiceJson != '[]'
            BEGIN
                PRINT '=== Calculating invoice totals...';
                
                DECLARE @hospitalAmount DECIMAL(18,4) = 0;
                DECLARE @physicianAmount DECIMAL(18,4) = 0;
                DECLARE @medicamentAmount DECIMAL(18,4) = 0;
                DECLARE @net DECIMAL(18,4) = 0;
                DECLARE @gross DECIMAL(18,4) = 0;

                SELECT
                    @hospitalAmount = ISNULL(SUM(CAST(JSON_VALUE(value, '$.unitPrice') AS DECIMAL(18,4)) * CAST(JSON_VALUE(value, '$.quantity') AS DECIMAL(18,4))), 0),
                    @net = ISNULL(SUM(CAST(JSON_VALUE(value, '$.netPrice') AS DECIMAL(18,4))), 0),
                    @gross = ISNULL(SUM(CAST(JSON_VALUE(value, '$.unitPrice') AS DECIMAL(18,4)) * CAST(JSON_VALUE(value, '$.quantity') AS DECIMAL(18,4))), 0)
                FROM OPENJSON(@invoiceJson)
                WHERE CAST(ISNULL(JSON_VALUE(value, '$.denomination'), '0') AS INT) > 0;

                PRINT '=== Inserting invoice header...';
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
                    'QuickAdmission', 1, 'QA', GETDATE(), @admissionId,
                    @hospitalAmount, @physicianAmount, @medicamentAmount,
                    1, 'Quick Admission Invoice', 1, 'USD', 1.0,
                    @checkInClass, 'Quick Admission', 1, 'Full Coverage', 100.0,
                    @referralPhysician, NULLIF(@attendingPhysician, 0), 1, 'Standard',
                    @net, @gross, @mrn, @firstName + ' ' + @lastName, @admissionNumber, @checkInDate,
                    NULLIF(@mainInsurance, 0), @admissionCreatedBy, GETDATE(), 0
                );

                SET @invoiceHeaderId = SCOPE_IDENTITY();
                PRINT '=== Invoice Header ID: ' + CAST(@invoiceHeaderId AS VARCHAR(10));

                PRINT '=== Inserting invoice details...';
                SET @stepTime = GETDATE();
                
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
                    GETDATE(),
                    CAST(ISNULL(JSON_VALUE(value, '$.medicationUnit'), '113') AS INT),
                    ISNULL(JSON_VALUE(value, '$.medicationUnitDescription'), 'Clinics'),
                    @admissionId,
                    @patientId,
                    CAST(JSON_VALUE(value, '$.denomination') AS INT),
                    ISNULL(JSON_VALUE(value, '$.denominationCode'), ''),
                    ISNULL(JSON_VALUE(value, '$.denominationDescription'), ''),
                    CAST(ISNULL(JSON_VALUE(value, '$.quantity'), '1') AS DECIMAL(18,4)),
                    CAST(ISNULL(JSON_VALUE(value, '$.unitPrice'), '0') AS DECIMAL(18,4)),
                    CAST(ISNULL(JSON_VALUE(value, '$.netPrice'), '0') AS DECIMAL(18,4)),
                    CAST(ISNULL(JSON_VALUE(value, '$.netUnitPrice'), '0') AS DECIMAL(18,4)),
                    0,
                    CAST(ISNULL(JSON_VALUE(value, '$.discount'), '0') AS DECIMAL(18,4)),
                    CAST(ISNULL(JSON_VALUE(value, '$.lumpSum'), '0') AS DECIMAL(18,4)),
                    CAST(ISNULL(JSON_VALUE(value, '$.operatingPhysician'), '0') AS INT),
                    CAST(ISNULL(JSON_VALUE(value, '$.costCenter'), '12') AS INT),
                    CAST(ISNULL(JSON_VALUE(value, '$.profitCenter'), '3') AS INT),
                    GETDATE(),
                    @admissionCreatedBy,
                    GETDATE(),
                    0
                FROM OPENJSON(@invoiceJson)
                WHERE CAST(ISNULL(JSON_VALUE(value, '$.denomination'), '0') AS INT) > 0;

                SET @elapsedSeconds = DATEDIFF(SECOND, @stepTime, GETDATE());
                PRINT '=== Invoice details inserted: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';
            END

            SET @elapsedSeconds = DATEDIFF(SECOND, @stepTime, GETDATE());
            PRINT '=== STEP 4 COMPLETE: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';
        END

        -- =====================================================
        -- 4. COMMIT TRANSACTION
        -- =====================================================
        SET @stepTime = GETDATE();
        PRINT '=== STEP 5: Committing transaction...';
        
        IF @transactionCount = 0
            ROLLBACK TRANSACTION; -- For testing - change to COMMIT when ready
        
        SET @elapsedSeconds = DATEDIFF(SECOND, @stepTime, GETDATE());
        PRINT '=== STEP 5 COMPLETE: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';

        -- Total elapsed time
        SET @elapsedSeconds = DATEDIFF(SECOND, @startTime, GETDATE());
        PRINT '=== TOTAL ELAPSED TIME: ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds';

        -- Return success result
        SELECT
            ISNULL(@mrn, '') AS MRN,
            @patientId AS PatientID,
            ISNULL(@admissionNumber, '') AS AdmissionNumber,
            @admissionId AS AdmissionID,
            @invoiceHeaderId AS InvoiceHeaderID,
            'Success' AS Status,
            CASE WHEN @errorMessage != '' THEN @errorMessage ELSE '' END AS ErrorMessage;

    END TRY
    BEGIN CATCH
        IF @transactionCount = 0 AND @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @errorMessage = ERROR_MESSAGE();
        
        SET @elapsedSeconds = DATEDIFF(SECOND, @startTime, GETDATE());
        PRINT '=== ERROR AFTER ' + CAST(@elapsedSeconds AS VARCHAR(10)) + ' seconds: ' + @errorMessage;

        SELECT
            '' AS MRN,
            NULL AS PatientID,
            '' AS AdmissionNumber,
            NULL AS AdmissionID,
            NULL AS InvoiceHeaderID,
            'Error' AS Status,
            @errorMessage AS ErrorMessage;

    END CATCH
END
GO

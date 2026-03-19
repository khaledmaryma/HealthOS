-- =====================================================
-- Stored Procedure: sp_SaveQuickAdmission (FIXED VERSION)
-- Purpose: Save complete quick admission data (Patient, Admission, Invoice)
-- Database: Admission (runs from AdmissionConnection)
-- Parameters:
--   @existingPatientId: ID of existing patient (NULL for new patient)
--   @saveData: JSON string containing patient, admission, and invoice data
--   @saveOptions: JSON string containing save options
-- Returns: Result set with MRN, PatientID, AdmissionNumber, AdmissionID, InvoiceHeaderID, Status, ErrorMessage
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
    DECLARE @department NVARCHAR(50); -- Changed to NVARCHAR for Department
    DECLARE @checkInDate DATETIME;
    DECLARE @checkOutDate DATETIME;
    DECLARE @type SMALLINT;
    DECLARE @isWorkAccident BIT;
    DECLARE @isExtended BIT;
    DECLARE @group INT;
    DECLARE @admissionCreatedBy INT;

    -- Start transaction if not already in one
    IF @transactionCount = 0
        BEGIN TRANSACTION;

    BEGIN TRY
        -- Parse save options - Extract from nested saveOptions if present
        DECLARE @optionsJson NVARCHAR(MAX);
        
        -- Check if saveOptions contains the full JSON (with nested saveOptions)
        IF @saveOptions IS NOT NULL AND @saveOptions != '' AND @saveOptions != 'null'
        BEGIN
            -- Try to extract nested saveOptions first
            SET @optionsJson = JSON_QUERY(@saveOptions, '$.saveOptions');
            
            -- If not found as nested, check if it's at root level
            IF @optionsJson IS NULL OR @optionsJson = 'null' OR @optionsJson = ''
            BEGIN
                -- Check if saveMedicalFile exists at root (means it's already the options JSON)
                IF JSON_VALUE(@saveOptions, '$.saveMedicalFile') IS NOT NULL
                BEGIN
                    SET @optionsJson = @saveOptions;
                END
                -- Otherwise try to get from saveData
                ELSE IF @saveData IS NOT NULL
                BEGIN
                    SET @optionsJson = JSON_QUERY(@saveData, '$.saveOptions');
                END
            END
        END
        ELSE
        BEGIN
            -- If saveOptions is null/empty, extract from saveData
            IF @saveData IS NOT NULL
            BEGIN
                SET @optionsJson = JSON_QUERY(@saveData, '$.saveOptions');
            END
        END
        
        -- Set defaults if still null
        IF @optionsJson IS NULL OR @optionsJson = '' OR @optionsJson = 'null'
        BEGIN
            SET @optionsJson = '{"saveMedicalFile":1,"saveAdmission":1,"saveInvoice":1}';
        END
        
        -- Parse the save options JSON
        SET @saveMedicalFile = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveMedicalFile'), '1') AS BIT);
        SET @saveAdmission = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveAdmission'), '1') AS BIT);
        SET @saveInvoice = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveInvoice'), '0') AS BIT);

        -- =====================================================
        -- 1. SAVE PATIENT (if requested and not existing patient)
        -- =====================================================
        IF @saveMedicalFile = 1 AND @existingPatientId IS NULL AND @saveData IS NOT NULL
        BEGIN
            -- Parse patient data from JSON
            SET @firstName = JSON_VALUE(@saveData, '$.patient.FirstName');
            SET @lastName = JSON_VALUE(@saveData, '$.patient.LastName');
            SET @middleName = JSON_VALUE(@saveData, '$.patient.MiddleName');
            SET @gender = JSON_VALUE(@saveData, '$.patient.Gender');
            SET @phone = JSON_VALUE(@saveData, '$.patient.Phone');
            SET @arabicFullName = JSON_VALUE(@saveData, '$.patient.ArabicFullName');
            
            -- Handle DOB - JSON_VALUE returns string, need to convert
            DECLARE @dobString NVARCHAR(50) = JSON_VALUE(@saveData, '$.patient.DOB');
            IF @dobString IS NOT NULL AND @dobString != ''
            BEGIN
                SET @dob = TRY_CAST(@dobString AS DATETIME);
            END
            
            SET @maritalStatus = CAST(ISNULL(JSON_VALUE(@saveData, '$.patient.MaritalStatus'), '0') AS SMALLINT);
            SET @createdBy = CAST(ISNULL(JSON_VALUE(@saveData, '$.patient.CreatedBy'), '338') AS INT);

            -- Insert patient record into HospitalDefinition database
            INSERT INTO HospitalDefinition.dbo.Patient (
                FirstName, LastName, MiddleName, Gender, Phone, 
                ArabicFullName, DOB, MaritalStatus, CreatedBy, CreatedDate, IsDeleted
            )
            VALUES (
                @firstName, @lastName, @middleName, @gender, @phone,
                @arabicFullName, @dob, @maritalStatus, @createdBy, GETDATE(), 0
            );

            -- Get the new patient ID
            SET @patientId = SCOPE_IDENTITY();

            -- Get or generate MRN
            SELECT @mrn = ISNULL(MedicalRecordNumber, CAST(@patientId AS NVARCHAR(50)))
            FROM HospitalDefinition.dbo.Patient
            WHERE ID = @patientId;

            -- Update MRN if it's null or empty
            IF @mrn IS NULL OR @mrn = ''
            BEGIN
                SET @mrn = CAST(@patientId AS NVARCHAR(50));
                UPDATE HospitalDefinition.dbo.Patient
                SET MedicalRecordNumber = @mrn
                WHERE ID = @patientId;
            END
        END
        ELSE IF @existingPatientId IS NOT NULL
        BEGIN
            SET @patientId = @existingPatientId;

            -- Get existing MRN and patient info
            SELECT 
                @mrn = ISNULL(MedicalRecordNumber, CAST(@patientId AS NVARCHAR(50))),
                @firstName = FirstName,
                @lastName = LastName,
                @middleName = MiddleName
            FROM HospitalDefinition.dbo.Patient
            WHERE ID = @patientId AND (IsDeleted = 0 OR IsDeleted IS NULL);
        END
        ELSE IF @saveMedicalFile = 0 AND @existingPatientId IS NULL
        BEGIN
            -- Cannot proceed without patient
            RAISERROR('Patient ID is required', 16, 1);
        END

        -- =====================================================
        -- 2. SAVE ADMISSION (if requested)
        -- =====================================================
        IF @saveAdmission = 1 AND @patientId IS NOT NULL AND @saveData IS NOT NULL
        BEGIN
            -- Parse admission data from JSON
            SET @admissionSite = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.AdmissionSite'), '0') AS INT);
            SET @referralPhysician = CAST(JSON_VALUE(@saveData, '$.admission.ReferralPhysician') AS INT);
            SET @attendingPhysician = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.AttendingPhysician'), CAST(JSON_VALUE(@saveData, '$.admission.ReferralPhysician') AS INT)) AS INT);
            SET @mainInsurance = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.MainInsurance'), '0') AS INT);
            SET @mainInsuranceClass = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.MainInsuranceClass'), '0') AS SMALLINT);
            SET @insured = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.Insured'), '0') AS SMALLINT);
            SET @auxiliaryInsurance = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.AuxiliaryInsurance'), '0') AS INT);
            SET @auxiliaryInsuranceClass = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.AuxiliaryInsuranceClass'), '0') AS SMALLINT);
            SET @checkInClass = CAST(ISNULL(JSON_VALUE(@saveData, '$.admission.CheckInClass'), '0') AS SMALLINT);
            SET @department = JSON_VALUE(@saveData, '$.admission.Department'); -- Department is a string, not INT
            
            -- Handle CheckInDate - JSON_VALUE returns string
            DECLARE @checkInDateString NVARCHAR(50) = JSON_VALUE(@saveData, '$.admission.CheckInDate');
            IF @checkInDateString IS NOT NULL AND @checkInDateString != ''
            BEGIN
                SET @checkInDate = TRY_CAST(@checkInDateString AS DATETIME);
                IF @checkInDate IS NULL
                    SET @checkInDate = GETDATE();
            END
            ELSE
                SET @checkInDate = GETDATE();
            
            -- Handle CheckOutDate
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

            -- Validate required fields
            IF @referralPhysician IS NULL OR @referralPhysician = 0
            BEGIN
                RAISERROR('Referral Physician is required', 16, 1);
            END

            IF @checkInDate IS NULL
            BEGIN
                RAISERROR('Check-in Date is required', 16, 1);
            END

            -- Generate admission number using stored procedure
            EXEC [dbo].[GenerateAdmissionNumber_V1] @checkInDate, @type, @admissionNumber OUTPUT;

            -- Insert admission record
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

            -- Get the new admission ID
            SET @admissionId = SCOPE_IDENTITY();

            -- =====================================================
            -- 2.1. SYNC ResidentPatient TABLE (after admission creation)
            -- =====================================================
            BEGIN TRY
                -- Get patient information for ResidentPatient sync
                DECLARE @patientMRN INT = 0;
                DECLARE @patientFirstName NVARCHAR(50) = '';
                DECLARE @patientLastName NVARCHAR(50) = '';
                DECLARE @patientMiddleName NVARCHAR(50) = '';
                DECLARE @patientFirstNameArabic NVARCHAR(50) = '';
                DECLARE @patientLastNameArabic NVARCHAR(50) = '';
                DECLARE @patientMiddleNameArabic NVARCHAR(50) = '';
                DECLARE @patientDOB DATETIME;
                DECLARE @patientGender NVARCHAR(1) = '';
                DECLARE @patientPhone NVARCHAR(50) = '';

                SELECT 
                    @patientMRN = ISNULL(CAST(MedicalRecordNumber AS INT), 0),
                    @patientFirstName = ISNULL(FirstName, ''),
                    @patientLastName = ISNULL(LastName, ''),
                    @patientMiddleName = ISNULL(MiddleName, ''),
                    @patientFirstNameArabic = ISNULL(FirstNameArabic, ''),
                    @patientLastNameArabic = ISNULL(LastNameArabic, ''),
                    @patientMiddleNameArabic = ISNULL(MiddleNameArabic, ''),
                    @patientDOB = ISNULL(DateOfBirth, DOB),
                    @patientGender = ISNULL(Gender, ''),
                    @patientPhone = ISNULL(Phone, '')
                FROM HospitalDefinition.dbo.Patient
                WHERE ID = @patientId AND (IsDeleted = 0 OR IsDeleted IS NULL);

                -- Calculate age
                DECLARE @age INT = NULL;
                IF @patientDOB IS NOT NULL
                BEGIN
                    DECLARE @today DATETIME = GETDATE();
                    SET @age = DATEDIFF(YEAR, @patientDOB, @today);
                    IF DATEADD(YEAR, @age, @patientDOB) > @today
                        SET @age = @age - 1;
                END

                -- Build patient name
                DECLARE @patientName NVARCHAR(200) = LTRIM(RTRIM(ISNULL(@patientFirstName, '') + ' ' + ISNULL(@patientMiddleName, '') + ' ' + ISNULL(@patientLastName, '')));
                DECLARE @patientArabicFullName NVARCHAR(200) = LTRIM(RTRIM(ISNULL(@patientFirstNameArabic, '') + ' ' + ISNULL(@patientMiddleNameArabic, '') + ' ' + ISNULL(@patientLastNameArabic, '')));

                -- Get lookup descriptions
                DECLARE @checkInClassDescription NVARCHAR(100) = '';
                DECLARE @mainInsuranceDescription NVARCHAR(100) = '';
                DECLARE @mainInsuranceClassDescription NVARCHAR(100) = '';
                DECLARE @referralPhysicianName NVARCHAR(100) = '';
                DECLARE @attendingPhysicianName NVARCHAR(100) = '';
                DECLARE @auxiliaryInsuranceDescription NVARCHAR(100) = '';
                DECLARE @auxiliaryInsuranceClassDescription NVARCHAR(100) = '';

                -- Get CheckInClass description
                IF @checkInClass > 0
                BEGIN
                    SELECT @checkInClassDescription = ISNULL(Description, '')
                    FROM HospitalDefinition.dbo.CheckInClass
                    WHERE ID = @checkInClass AND (IsDeleted = 0 OR IsDeleted IS NULL);
                END

                -- Get MainInsurance description
                IF @mainInsurance > 0
                BEGIN
                    SELECT @mainInsuranceDescription = ISNULL(Description, '')
                    FROM HospitalDefinition.dbo.Insurance
                    WHERE ID = @mainInsurance AND (IsDeleted = 0 OR IsDeleted IS NULL);
                END

                -- Get MainInsuranceClass description
                IF @mainInsuranceClass > 0
                BEGIN
                    SELECT @mainInsuranceClassDescription = ISNULL(Description, '')
                    FROM HospitalDefinition.dbo.InsuranceClass
                    WHERE ID = @mainInsuranceClass AND (IsDeleted = 0 OR IsDeleted IS NULL);
                END

                -- Get ReferralPhysician name
                IF @referralPhysician > 0
                BEGIN
                    SELECT @referralPhysicianName = ISNULL(Name, '')
                    FROM HospitalDefinition.dbo.Physician
                    WHERE ID = @referralPhysician AND (IsDeleted = 0 OR IsDeleted IS NULL);
                END

                -- Get AttendingPhysician name
                IF @attendingPhysician > 0
                BEGIN
                    SELECT @attendingPhysicianName = ISNULL(Name, '')
                    FROM HospitalDefinition.dbo.Physician
                    WHERE ID = @attendingPhysician AND (IsDeleted = 0 OR IsDeleted IS NULL);
                END

                -- Get AuxiliaryInsurance description
                IF @auxiliaryInsurance > 0
                BEGIN
                    SELECT @auxiliaryInsuranceDescription = ISNULL(Description, '')
                    FROM HospitalDefinition.dbo.Insurance
                    WHERE ID = @auxiliaryInsurance AND (IsDeleted = 0 OR IsDeleted IS NULL);
                END

                -- Get AuxiliaryInsuranceClass description
                IF @auxiliaryInsuranceClass > 0
                BEGIN
                    SELECT @auxiliaryInsuranceClassDescription = ISNULL(Description, '')
                    FROM HospitalDefinition.dbo.InsuranceClass
                    WHERE ID = @auxiliaryInsuranceClass AND (IsDeleted = 0 OR IsDeleted IS NULL);
                END

                -- Check if ResidentPatient record already exists
                DECLARE @existingResidentPatientId INT = NULL;
                SELECT @existingResidentPatientId = ID
                FROM dbo.ResidentPatient
                WHERE Admission = @admissionId AND (IsDeleted = 0 OR IsDeleted IS NULL);

                IF @existingResidentPatientId IS NOT NULL
                BEGIN
                    -- Update existing record
                    UPDATE dbo.ResidentPatient
                    SET PatientID = @patientId,
                        MRN = @patientMRN,
                        AdmissionNumber = @admissionNumber,
                        PatientName = @patientName,
                        ArabicFullName = NULLIF(@patientArabicFullName, ''),
                        MedicalRecordNumber = @mrn,
                        PatientDOB = @patientDOB,
                        Age = @age,
                        PatientGender = @patientGender,
                        CheckInDate = @checkInDate,
                        CheckInClassID = @checkInClass,
                        CheckInClassDescription = @checkInClassDescription,
                        MainInsuranceID = @mainInsurance,
                        MainInsuranceDescription = @mainInsuranceDescription,
                        MainInsuranceClassID = @mainInsuranceClass,
                        MainInsuranceClassDescription = @mainInsuranceClassDescription,
                        ReferralPhysicianID = @referralPhysician,
                        ReferralPhysicianName = @referralPhysicianName,
                        AttendingPhysicianID = NULLIF(@attendingPhysician, 0),
                        AttendingPhysicianName = NULLIF(@attendingPhysicianName, ''),
                        MedicationUnitID = 113,
                        MedicationUnitDescription = 'Clinics',
                        InsuranceID = @mainInsurance,
                        InsuranceDescription = @mainInsuranceDescription,
                        GuarantorID = 0,
                        GuarantorDescription = '',
                        CurrencyID = 2,
                        CurrencyDescription = 'USD',
                        ClassID = @checkInClass,
                        ClassDescription = @checkInClassDescription,
                        ContextPriceID = 0,
                        ContextPriceDescription = '',
                        ContextEnumerationID = 0,
                        ContextEnumerationDescription = '',
                        AdmissionType = @type,
                        AdmissionTypeDescription = '',
                        Contact = NULLIF(@patientPhone, ''),
                        AuxiliaryInsuranceID = NULLIF(@auxiliaryInsurance, 0),
                        AuxiliaryInsuranceDescription = NULLIF(@auxiliaryInsuranceDescription, ''),
                        AuxiliaryInsuranceClassID = NULLIF(@auxiliaryInsuranceClass, 0),
                        AuxiliaryInsuranceClassDescription = NULLIF(@auxiliaryInsuranceClassDescription, ''),
                        AdmissionSite = NULLIF(@admissionSite, 0),
                        [Group] = NULLIF(@group, 0),
                        ModifiedBy = @admissionCreatedBy,
                        ModifiedDate = GETDATE()
                    WHERE ID = @existingResidentPatientId;
                END
                ELSE
                BEGIN
                    -- Insert new record
                    INSERT INTO dbo.ResidentPatient (
                        PatientID, Admission, MRN, AdmissionNumber, PatientName, ArabicFullName,
                        MedicalRecordNumber, PatientDOB, Age, PatientGender, CheckInDate,
                        CheckInClassID, CheckInClassDescription, MainInsuranceID, MainInsuranceDescription,
                        MainInsuranceClassID, MainInsuranceClassDescription, ReferralPhysicianID, ReferralPhysicianName,
                        AttendingPhysicianID, AttendingPhysicianName, MedicationUnitID, MedicationUnitDescription,
                        InsuranceID, InsuranceDescription, GuarantorID, GuarantorDescription,
                        CurrencyID, CurrencyDescription, ClassID, ClassDescription,
                        ContextPriceID, ContextPriceDescription, ContextEnumerationID, ContextEnumerationDescription,
                        AdmissionType, AdmissionTypeDescription, Contact, AuxiliaryInsuranceID, AuxiliaryInsuranceDescription,
                        AuxiliaryInsuranceClassID, AuxiliaryInsuranceClassDescription, IsDischarged, DischargeDate,
                        AdmissionSite, [Group], IsDeleted, CreatedBy, CreatedDate
                    )
                    VALUES (
                        @patientId, @admissionId, @patientMRN, @admissionNumber, @patientName, NULLIF(@patientArabicFullName, ''),
                        @mrn, @patientDOB, @age, @patientGender, @checkInDate,
                        @checkInClass, @checkInClassDescription, @mainInsurance, @mainInsuranceDescription,
                        @mainInsuranceClass, @mainInsuranceClassDescription, @referralPhysician, @referralPhysicianName,
                        NULLIF(@attendingPhysician, 0), NULLIF(@attendingPhysicianName, ''), 113, 'Clinics',
                        @mainInsurance, @mainInsuranceDescription, 0, '',
                        2, 'USD', @checkInClass, @checkInClassDescription,
                        0, '', 0, '',
                        @type, '', NULLIF(@patientPhone, ''), NULLIF(@auxiliaryInsurance, 0), NULLIF(@auxiliaryInsuranceDescription, ''),
                        NULLIF(@auxiliaryInsuranceClass, 0), NULLIF(@auxiliaryInsuranceClassDescription, ''), 0, NULL,
                        NULLIF(@admissionSite, 0), NULLIF(@group, 0), 0, @admissionCreatedBy, GETDATE()
                    );
                END
            END TRY
            BEGIN CATCH
                SET @errorMessage = @errorMessage + 'Warning: Error syncing ResidentPatient: ' + ERROR_MESSAGE() + '; ';
            END CATCH
        END

        -- =====================================================
        -- 3. SAVE INVOICE (if requested)
        -- =====================================================
        IF @saveInvoice = 1 AND @admissionId IS NOT NULL AND @saveData IS NOT NULL
        BEGIN
            -- Get invoice JSON once
            DECLARE @invoiceJson NVARCHAR(MAX) = JSON_QUERY(@saveData, '$.invoice');
            
            -- Check if invoice array exists and is valid (FAST CHECK - no OPENJSON scan)
            IF @invoiceJson IS NOT NULL AND @invoiceJson != 'null' AND @invoiceJson != '' AND @invoiceJson != '[]'
            BEGIN
                -- Calculate totals using OPENJSON (only once)
                DECLARE @hospitalAmount DECIMAL(18,4) = 0;
                DECLARE @physicianAmount DECIMAL(18,4) = 0;
                DECLARE @medicamentAmount DECIMAL(18,4) = 0;
                DECLARE @net DECIMAL(18,4) = 0;
                DECLARE @gross DECIMAL(18,4) = 0;

                -- Calculate totals in a single OPENJSON call
                SELECT
                    @hospitalAmount = ISNULL(SUM(CAST(JSON_VALUE(value, '$.unitPrice') AS DECIMAL(18,4)) * CAST(JSON_VALUE(value, '$.quantity') AS DECIMAL(18,4))), 0),
                    @net = ISNULL(SUM(CAST(JSON_VALUE(value, '$.netPrice') AS DECIMAL(18,4))), 0),
                    @gross = ISNULL(SUM(CAST(JSON_VALUE(value, '$.unitPrice') AS DECIMAL(18,4)) * CAST(JSON_VALUE(value, '$.quantity') AS DECIMAL(18,4))), 0)
                FROM OPENJSON(@invoiceJson)
                WHERE CAST(ISNULL(JSON_VALUE(value, '$.denomination'), '0') AS INT) > 0;

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
                    'QuickAdmission', 1, 'QA', GETDATE(), @admissionId,
                    @hospitalAmount, @physicianAmount, @medicamentAmount,
                    1, 'Quick Admission Invoice', 1, 'USD', 1.0,
                    @checkInClass, ISNULL(@checkInClassDescription, 'Quick Admission'), 1, 'Full Coverage', 100.0,
                    @referralPhysician, NULLIF(@attendingPhysician, 0), 1, 'Standard',
                    @net, @gross, @mrn, @patientName, @admissionNumber, @checkInDate,
                    NULLIF(@mainInsurance, 0), @admissionCreatedBy, GETDATE(), 0
                );

                SET @invoiceHeaderId = SCOPE_IDENTITY();

                -- Insert invoice details (single OPENJSON call - reuse @invoiceJson)
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
            END
        END

        -- =====================================================
        -- 4. COMMIT TRANSACTION AND RETURN RESULTS
        -- =====================================================
        IF @transactionCount = 0
            COMMIT TRANSACTION;

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
        -- Rollback transaction on error
        IF @transactionCount = 0 AND @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Get error details
        SET @errorMessage = ERROR_MESSAGE();

        -- Return error result
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



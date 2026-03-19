USE HospitalDefinition;
GO

-- Drop existing trigger if it exists
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Patient_AutoGenerateMRN')
BEGIN
    DROP TRIGGER trg_Patient_AutoGenerateMRN;
END
GO

-- Create trigger to auto-generate MRN before insert
CREATE TRIGGER trg_Patient_AutoGenerateMRN
ON Patient
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NewMRN INT;
    
    -- Get the next MRN from Configuration database and increment it
    UPDATE Configuration.dbo.TransactionSequenceControl
    SET LastMedicalRecordNumber = LastMedicalRecordNumber + 1,
        @NewMRN = LastMedicalRecordNumber + 1
    WHERE ID = 1;
    
    -- Insert the patient record with the new MRN
    INSERT INTO Patient (
        MedicalRecordNumber,
        FirstName,
        LastName,
        MiddleName,
        MaidenName,
        DOB,
        Gender,
        MaritalStatus,
        Profession,
        Nationality,
        IDNumber,
        Employee,
        HasImage,
        RegisterNumber,
        RegisterPlace,
        RegisterPlaceA,
        Phone,
        Address,
        BloodGroupID,
        ConfidentialityID,
        IsDeleted,
        CreatedBy,
        ModifiedBy,
        CreatedDate,
        ModifiedDate,
        Image,
        ArabicFullName,
        MiddleNameA,
        OldMedicalRecordNumber,
        FullName,
        Name,
        GUID,
        ContactName,
        ContactPhone,
        ContactType
    )
    SELECT 
        CAST(@NewMRN AS NVARCHAR(50)),  -- Auto-generated MRN
        FirstName,
        LastName,
        MiddleName,
        MaidenName,
        DOB,
        Gender,
        MaritalStatus,
        Profession,
        Nationality,
        IDNumber,
        Employee,
        HasImage,
        RegisterNumber,
        RegisterPlace,
        RegisterPlaceA,
        Phone,
        Address,
        BloodGroupID,
        ConfidentialityID,
        IsDeleted,
        CreatedBy,
        ModifiedBy,
        GETDATE(),  -- CreatedDate
        ModifiedDate,
        Image,
        ArabicFullName,
        MiddleNameA,
        OldMedicalRecordNumber,
        FullName,
        Name,
        GUID,
        ContactName,
        ContactPhone,
        ContactType
    FROM inserted;
END
GO

PRINT 'Trigger trg_Patient_AutoGenerateMRN created successfully!';
GO





















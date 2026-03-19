-- =====================================================
-- Test Admission Number Generation Trigger
-- =====================================================

USE Admission;
GO

-- Test the trigger with different admission types
PRINT 'Testing Admission Number Generation Trigger...';
PRINT '';

-- Test 1: InPatient (Type = 1)
PRINT 'Test 1: Creating InPatient admission (Type = 1)';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Clinic', 
    '2024-08-15', 1, 1, 0, 0, 22, 338, GETDATE(), 0
);

-- Test 2: CreditOutPatient (Type = 2)
PRINT 'Test 2: Creating CreditOutPatient admission (Type = 2)';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Lab', 
    '2024-08-15', 2, 2, 0, 0, 22, 338, GETDATE(), 0
);

-- Test 3: CashOutPatient (Type = 3)
PRINT 'Test 3: Creating CashOutPatient admission (Type = 3)';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Radio', 
    '2024-08-15', 3, 3, 0, 0, 22, 338, GETDATE(), 0
);

-- Test 4: Reservation (Type = 4)
PRINT 'Test 4: Creating Reservation admission (Type = 4)';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Beauty', 
    '2024-08-15', 4, 4, 0, 0, 22, 338, GETDATE(), 0
);

-- Test 5: Test with existing number (should not be overwritten)
PRINT 'Test 5: Creating admission with existing number (should not be overwritten)';
INSERT INTO Admission.dbo.Admission (
    Number, AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    'MANUAL.12345.08.24', 3, 1, 1, 5, 4, 0, 5, 4, 5, 'Clinic', 
    '2024-08-15', 5, 1, 0, 0, 22, 338, GETDATE(), 0
);

-- Show results
PRINT '';
PRINT 'Results:';
PRINT '========';

SELECT 
    ID,
    Number,
    Type,
    CheckInDate,
    Department,
    Patient,
    CASE Type
        WHEN 1 THEN 'InPatient'
        WHEN 2 THEN 'CreditOutPatient'
        WHEN 3 THEN 'CashOutPatient'
        WHEN 4 THEN 'Reservation'
        ELSE 'Unknown'
    END AS TypeDescription
FROM Admission.dbo.Admission 
WHERE ID IN (
    SELECT TOP 5 ID FROM Admission.dbo.Admission 
    ORDER BY ID DESC
)
ORDER BY ID DESC;

-- Show current counter values
PRINT '';
PRINT 'Current Counter Values:';
PRINT '======================';

SELECT 
    InPatient,
    CreditOutPatient,
    CashOutPatient,
    Reservation,
    ModifiedDate
FROM AdmissionCounter;

PRINT '';
PRINT 'Test completed successfully!';
GO

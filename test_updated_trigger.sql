-- =====================================================
-- Test Updated Admission Number Generation Trigger
-- =====================================================

USE Admission;
GO

-- Test the updated trigger
PRINT 'Testing Updated Admission Number Generation Trigger...';
PRINT '';

-- Test 1: InPatient (Type = 1) for August 2024
PRINT 'Test 1: Creating InPatient admission (Type = 1) for August 2024';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Clinic', 
    '2024-08-15', 100, 1, 0, 0, 22, 338, GETDATE(), 0
);

-- Test 2: CreditOutPatient (Type = 2) for August 2024
PRINT 'Test 2: Creating CreditOutPatient admission (Type = 2) for August 2024';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Lab', 
    '2024-08-15', 101, 2, 0, 0, 22, 338, GETDATE(), 0
);

-- Test 3: CashOutPatient (Type = 3) for August 2024
PRINT 'Test 3: Creating CashOutPatient admission (Type = 3) for August 2024';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Radio', 
    '2024-08-15', 102, 3, 0, 0, 22, 338, GETDATE(), 0
);

-- Test 4: Reservation (Type = 4) for August 2024
PRINT 'Test 4: Creating Reservation admission (Type = 4) for August 2024';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Beauty', 
    '2024-08-15', 103, 4, 0, 0, 22, 338, GETDATE(), 0
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
    'MANUAL.99999.08.24', 3, 1, 1, 5, 4, 0, 5, 4, 5, 'Clinic', 
    '2024-08-15', 104, 1, 0, 0, 22, 338, GETDATE(), 0
);

-- Test 6: Test for a year/month without counter record (should show warning)
PRINT 'Test 6: Creating admission for year/month without counter record (should show warning)';
INSERT INTO Admission.dbo.Admission (
    AdmissionSite, ReferralPhysician, AttendingPhysician, MainInsurance, 
    MainInsuranceClass, Insured, AuxiliaryInsurance, AuxiliaryInsuranceClass, 
    CheckInClass, Department, CheckInDate, Patient, Type, IsWorkAccident, 
    IsExtended, [Group], CreatedBy, CreatedDate, IsDeleted
)
VALUES (
    3, 1, 1, 5, 4, 0, 5, 4, 5, 'Clinic', 
    '2020-01-15', 105, 1, 0, 0, 22, 338, GETDATE(), 0
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
    SELECT TOP 6 ID FROM Admission.dbo.Admission 
    ORDER BY ID DESC
)
ORDER BY ID DESC;

-- Show updated counter values for August 2024
PRINT '';
PRINT 'Updated Counter Values for August 2024:';
PRINT '======================================';

SELECT 
    Year,
    Month,
    InPatient,
    CreditOutPatient,
    CashOutPatient,
    Reservation,
    ModifiedDate
FROM AdmissionCounter 
WHERE Year = 2024 AND Month = 8;

PRINT '';
PRINT 'Test completed successfully!';
PRINT 'Note: Test 6 should show a warning about missing counter record.';
GO
















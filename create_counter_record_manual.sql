-- =====================================================
-- Manual Counter Record Creation Script
-- =====================================================
-- Use this script to manually create counter records
-- for specific year/month combinations when needed
-- =====================================================

USE Admission;
GO

-- Function to create counter record for specific year/month
-- Usage: EXEC CreateCounterRecord @Year = 2024, @Month = 8
CREATE OR ALTER PROCEDURE CreateCounterRecord
    @Year INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if record already exists
    IF EXISTS (SELECT 1 FROM AdmissionCounter WHERE Year = @Year AND Month = @Month)
    BEGIN
        PRINT 'Counter record already exists for year ' + CAST(@Year AS NVARCHAR(4)) + ', month ' + CAST(@Month AS NVARCHAR(2));
        RETURN;
    END
    
    -- Create new counter record
    INSERT INTO AdmissionCounter (
        Year, Month, InPatient, CreditOutPatient, CashOutPatient, 
        Reservation, Temporary, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES (
        @Year, @Month, 0, 0, 0, 0, 0, 0, 338, GETDATE()
    );
    
    PRINT 'Counter record created successfully for year ' + CAST(@Year AS NVARCHAR(4)) + ', month ' + CAST(@Month AS NVARCHAR(2));
END
GO

-- Create counter records for current year (2024) - all months
PRINT 'Creating counter records for year 2024...';

EXEC CreateCounterRecord @Year = 2024, @Month = 1;
EXEC CreateCounterRecord @Year = 2024, @Month = 2;
EXEC CreateCounterRecord @Year = 2024, @Month = 3;
EXEC CreateCounterRecord @Year = 2024, @Month = 4;
EXEC CreateCounterRecord @Year = 2024, @Month = 5;
EXEC CreateCounterRecord @Year = 2024, @Month = 6;
EXEC CreateCounterRecord @Year = 2024, @Month = 7;
EXEC CreateCounterRecord @Year = 2024, @Month = 8;
EXEC CreateCounterRecord @Year = 2024, @Month = 9;
EXEC CreateCounterRecord @Year = 2024, @Month = 10;
EXEC CreateCounterRecord @Year = 2024, @Month = 11;
EXEC CreateCounterRecord @Year = 2024, @Month = 12;

-- Create counter records for next year (2025) - all months
PRINT 'Creating counter records for year 2025...';

EXEC CreateCounterRecord @Year = 2025, @Month = 1;
EXEC CreateCounterRecord @Year = 2025, @Month = 2;
EXEC CreateCounterRecord @Year = 2025, @Month = 3;
EXEC CreateCounterRecord @Year = 2025, @Month = 4;
EXEC CreateCounterRecord @Year = 2025, @Month = 5;
EXEC CreateCounterRecord @Year = 2025, @Month = 6;
EXEC CreateCounterRecord @Year = 2025, @Month = 7;
EXEC CreateCounterRecord @Year = 2025, @Month = 8;
EXEC CreateCounterRecord @Year = 2025, @Month = 9;
EXEC CreateCounterRecord @Year = 2025, @Month = 10;
EXEC CreateCounterRecord @Year = 2025, @Month = 11;
EXEC CreateCounterRecord @Year = 2025, @Month = 12;

-- Show current counter records
PRINT '';
PRINT 'Current counter records:';
PRINT '======================';

SELECT 
    Year,
    Month,
    InPatient,
    CreditOutPatient,
    CashOutPatient,
    Reservation,
    CreatedDate
FROM AdmissionCounter 
WHERE Year >= 2024
ORDER BY Year, Month;

PRINT '';
PRINT 'Counter records created successfully!';
PRINT 'The admission trigger will now work for 2024 and 2025.';
GO
















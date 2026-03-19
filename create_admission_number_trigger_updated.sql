-- =====================================================
-- Admission Number Generation Trigger (Updated for existing table structure)
-- =====================================================
-- This trigger automatically generates admission numbers
-- Format: XX.YYYYY.MM.YY
-- XX = Admission Type (01, 02, 03, 04)
-- YYYYY = Counter (5 digits with leading zeros)
-- MM = Check-in Month (2 digits)
-- YY = Check-in Year (2 digits)
-- =====================================================

USE Admission;
GO

-- Drop existing trigger if it exists
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Admission_GenerateNumber')
BEGIN
    DROP TRIGGER TR_Admission_GenerateNumber;
    PRINT 'Existing trigger dropped';
END
GO

-- Create the trigger
CREATE TRIGGER TR_Admission_GenerateNumber
ON Admission.dbo.Admission
FOR INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AdmissionID INT;
    DECLARE @AdmissionType INT;
    DECLARE @CheckInDate DATETIME;
    DECLARE @CurrentCounter INT;
    DECLARE @NewCounter INT;
    DECLARE @AdmissionNumber NVARCHAR(20);
    DECLARE @TypePrefix NVARCHAR(2);
    DECLARE @MonthPart NVARCHAR(2);
    DECLARE @YearPart NVARCHAR(2);
    DECLARE @CounterPart NVARCHAR(5);
    DECLARE @CheckInYear INT;
    DECLARE @CheckInMonth INT;
    
    -- Get the inserted record details
    SELECT 
        @AdmissionID = ID,
        @AdmissionType = Type,
        @CheckInDate = CheckInDate
    FROM inserted;
    
    -- Only generate number if Number field is NULL or empty
    IF EXISTS (SELECT 1 FROM inserted WHERE Number IS NULL OR Number = '')
    BEGIN
        -- Determine type prefix
        SET @TypePrefix = CASE @AdmissionType
            WHEN 1 THEN '01'  -- InPatient
            WHEN 2 THEN '02'  -- CreditOutPatient
            WHEN 3 THEN '03'  -- CashOutPatient
            WHEN 4 THEN '04'  -- Reservation
            ELSE '00'         -- Unknown type
        END;
        
        -- Extract month and year from CheckInDate
        SET @CheckInYear = YEAR(@CheckInDate);
        SET @CheckInMonth = MONTH(@CheckInDate);
        SET @MonthPart = RIGHT('0' + CAST(@CheckInMonth AS NVARCHAR(2)), 2);
        SET @YearPart = RIGHT(CAST(@CheckInYear AS NVARCHAR(4)), 2);
        
        -- Get current counter and increment based on admission type
        BEGIN TRANSACTION;
        
        BEGIN TRY
            -- Check if counter record exists for this year/month
            IF NOT EXISTS (SELECT 1 FROM AdmissionCounter WHERE Year = @CheckInYear AND Month = @CheckInMonth)
            BEGIN
                -- Create new counter record for this year/month
                INSERT INTO AdmissionCounter (Year, Month, InPatient, CreditOutPatient, CashOutPatient, Reservation, Temporary, IsDeleted, CreatedBy, CreatedDate)
                VALUES (@CheckInYear, @CheckInMonth, 0, 0, 0, 0, 0, 0, 338, GETDATE());
            END
            
            -- Get current counter value for this year/month
            SELECT @CurrentCounter = CASE @AdmissionType
                WHEN 1 THEN InPatient
                WHEN 2 THEN CreditOutPatient
                WHEN 3 THEN CashOutPatient
                WHEN 4 THEN Reservation
                ELSE 0
            END
            FROM AdmissionCounter WITH (UPDLOCK, HOLDLOCK)
            WHERE Year = @CheckInYear AND Month = @CheckInMonth;
            
            -- Increment counter
            SET @NewCounter = @CurrentCounter + 1;
            
            -- Update the counter table
            UPDATE AdmissionCounter 
            SET 
                InPatient = CASE WHEN @AdmissionType = 1 THEN @NewCounter ELSE InPatient END,
                CreditOutPatient = CASE WHEN @AdmissionType = 2 THEN @NewCounter ELSE CreditOutPatient END,
                CashOutPatient = CASE WHEN @AdmissionType = 3 THEN @NewCounter ELSE CashOutPatient END,
                Reservation = CASE WHEN @AdmissionType = 4 THEN @NewCounter ELSE Reservation END,
                ModifiedDate = GETDATE()
            WHERE Year = @CheckInYear AND Month = @CheckInMonth;
            
            -- Format counter with leading zeros (5 digits)
            SET @CounterPart = REPLICATE('0', 5 - LEN(CAST(@NewCounter AS NVARCHAR(10)))) + CAST(@NewCounter AS NVARCHAR(10));
            
            -- Build admission number: XX.YYYYY.MM.YY
            SET @AdmissionNumber = @TypePrefix + '.' + @CounterPart + '.' + @MonthPart + '.' + @YearPart;
            
            -- Update the admission record with the generated number
            UPDATE Admission.dbo.Admission 
            SET Number = @AdmissionNumber
            WHERE ID = @AdmissionID;
            
            COMMIT TRANSACTION;
            
            PRINT 'Admission number generated: ' + @AdmissionNumber;
            
        END TRY
        BEGIN CATCH
            ROLLBACK TRANSACTION;
            
            DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
            DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
            DECLARE @ErrorState INT = ERROR_STATE();
            
            PRINT 'Error generating admission number: ' + @ErrorMessage;
            
            -- Re-raise the error
            RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        END CATCH
    END
    ELSE
    BEGIN
        PRINT 'Admission number already provided, skipping generation';
    END
END
GO

PRINT 'Admission number generation trigger created successfully';
PRINT 'Trigger will generate numbers in format: XX.YYYYY.MM.YY';
PRINT 'Where:';
PRINT '  XX = Admission Type (01=InPatient, 02=CreditOutPatient, 03=CashOutPatient, 04=Reservation)';
PRINT '  YYYYY = Sequential counter (5 digits with leading zeros)';
PRINT '  MM = Check-in month (2 digits)';
PRINT '  YY = Check-in year (2 digits)';
PRINT 'Counters are maintained per year/month combination';
GO
















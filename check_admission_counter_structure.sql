-- Check AdmissionCounter table structure
USE Admission;
GO

-- Check if AdmissionCounter table exists
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AdmissionCounter')
BEGIN
    PRINT 'AdmissionCounter table exists';
    
    -- Get table structure
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_MAXIMUM_LENGTH,
        IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AdmissionCounter'
    ORDER BY ORDINAL_POSITION;
    
    -- Get sample data
    SELECT TOP 5 * FROM AdmissionCounter;
    
    -- Get row count
    SELECT COUNT(*) AS Total_Rows FROM AdmissionCounter;
END
ELSE
BEGIN
    PRINT 'AdmissionCounter table does not exist - will need to create it';
    
    -- Create the AdmissionCounter table
    CREATE TABLE AdmissionCounter (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        InPatient INT NOT NULL DEFAULT 0,
        CreditOutPatient INT NOT NULL DEFAULT 0,
        CashOutPatient INT NOT NULL DEFAULT 0,
        Reservation INT NOT NULL DEFAULT 0,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedDate DATETIME NOT NULL DEFAULT GETDATE()
    );
    
    -- Insert initial record
    INSERT INTO AdmissionCounter (InPatient, CreditOutPatient, CashOutPatient, Reservation)
    VALUES (0, 0, 0, 0);
    
    PRINT 'AdmissionCounter table created with initial record';
END
GO
















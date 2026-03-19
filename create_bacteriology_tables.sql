-- Create PatientLabBacteriology Tables if they don't exist
-- This script creates the necessary tables for bacteriology/antibiogram data

USE LIS;
GO

PRINT '====================================================================';
PRINT 'Creating Bacteriology Tables';
PRINT '====================================================================';
PRINT '';

-- =====================================================================
-- 1. Create PatientLabBacteriologyHeader table
-- =====================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PatientLabBacteriologyHeader')
BEGIN
    PRINT '1. Creating PatientLabBacteriologyHeader table...';
    
    CREATE TABLE PatientLabBacteriologyHeader (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        PatientLabResultID INT NOT NULL,  -- Links to PatientLabResult (Antibiogram test)
        Comments NVARCHAR(MAX),
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedBy INT,
        CreatedDate DATETIME,
        ModifiedBy INT,
        ModifiedDate DATETIME,
        CONSTRAINT FK_PatientLabBacteriologyHeader_PatientLabResult 
            FOREIGN KEY (PatientLabResultID) REFERENCES PatientLabResult(ID)
    );
    
    CREATE INDEX IX_PatientLabBacteriologyHeader_PatientLabResultID 
        ON PatientLabBacteriologyHeader(PatientLabResultID);
    
    PRINT '✓ PatientLabBacteriologyHeader table created successfully';
END
ELSE
BEGIN
    PRINT '⚠ PatientLabBacteriologyHeader table already exists';
    
    -- Check if PatientLabResultID column exists
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'PatientLabBacteriologyHeader' 
        AND COLUMN_NAME = 'PatientLabResultID'
    )
    BEGIN
        PRINT '  → Adding PatientLabResultID column...';
        ALTER TABLE PatientLabBacteriologyHeader ADD PatientLabResultID INT NULL;
        PRINT '  ✓ PatientLabResultID column added';
    END
END

PRINT '';

-- =====================================================================
-- 2. Create PatientLabBacteriology table
-- =====================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PatientLabBacteriology')
BEGIN
    PRINT '2. Creating PatientLabBacteriology table...';
    
    CREATE TABLE PatientLabBacteriology (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        BacteriologyHeaderID INT,
        GermID INT,
        GermDescription NVARCHAR(255),
        AntibioticID INT,
        AntibioticDescription NVARCHAR(255),
        Sensitivity NVARCHAR(50),      -- S, R, I (Sensitive, Resistant, Intermediate)
        Result NVARCHAR(50),            -- Numeric or text result
        Colony NVARCHAR(255),           -- Colony count/description
        DisplayOrder INT,
        Comments NVARCHAR(MAX),
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedBy INT,
        CreatedDate DATETIME,
        ModifiedBy INT,
        ModifiedDate DATETIME,
        CONSTRAINT FK_PatientLabBacteriology_Header 
            FOREIGN KEY (BacteriologyHeaderID) REFERENCES PatientLabBacteriologyHeader(ID),
        CONSTRAINT FK_PatientLabBacteriology_Germ 
            FOREIGN KEY (GermID) REFERENCES Germs(ID),
        CONSTRAINT FK_PatientLabBacteriology_Antibiotic 
            FOREIGN KEY (AntibioticID) REFERENCES Antibiotic(ID)
    );
    
    CREATE INDEX IX_PatientLabBacteriology_HeaderID 
        ON PatientLabBacteriology(BacteriologyHeaderID);
    CREATE INDEX IX_PatientLabBacteriology_GermID 
        ON PatientLabBacteriology(GermID);
    CREATE INDEX IX_PatientLabBacteriology_AntibioticID 
        ON PatientLabBacteriology(AntibioticID);
    
    PRINT '✓ PatientLabBacteriology table created successfully';
END
ELSE
BEGIN
    PRINT '⚠ PatientLabBacteriology table already exists';
END

PRINT '';
PRINT '====================================================================';
PRINT 'Table Creation Complete!';
PRINT '====================================================================';
PRINT '';
PRINT 'Summary:';
PRINT '  - PatientLabBacteriologyHeader: Links to PatientLabResult (Antibiogram test)';
PRINT '  - PatientLabBacteriology: Stores antibiotic sensitivity results';
PRINT '';
PRINT 'You can now use the API to create bacteriology records!';
PRINT '====================================================================';
GO

























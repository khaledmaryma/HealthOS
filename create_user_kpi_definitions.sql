-- Run against HISUsers database (same as UserDefinition / profile tables).
IF OBJECT_ID(N'dbo.UserKpiDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserKpiDefinition (
        Id INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        AppKey NVARCHAR(50) NOT NULL,
        HomePageId NVARCHAR(100) NOT NULL CONSTRAINT DF_UserKpi_HomePage DEFAULT (N'main'),
        Title NVARCHAR(200) NOT NULL,
        SqlQuery NVARCHAR(MAX) NOT NULL,
        DisplayMode INT NOT NULL CONSTRAINT DF_UserKpi_Display DEFAULT (0),
        GridShowTotals BIT NOT NULL CONSTRAINT DF_UserKpi_Totals DEFAULT (1),
        ChartOptionsJson NVARCHAR(MAX) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_UserKpi_Sort DEFAULT (0),
        CreatedUtc DATETIME2 NOT NULL,
        ModifiedUtc DATETIME2 NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_UserKpi_Del DEFAULT (0)
    );

    CREATE INDEX IX_UserKpi_User_App_Home ON dbo.UserKpiDefinition (UserId, AppKey, HomePageId)
        WHERE IsDeleted = 0;
END
GO

USE [HISUsers]
GO

IF OBJECT_ID('dbo.AppDefinition', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppDefinition](
        [ID] [int] IDENTITY(1,1) NOT NULL,
        [Code] [nvarchar](100) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [IsDeleted] [bit] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime] NOT NULL,
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime] NULL,
     CONSTRAINT [PK_AppDefinition] PRIMARY KEY CLUSTERED ([ID] ASC)
    ) ON [PRIMARY]
END
GO

IF OBJECT_ID('dbo.ScreenDefinition', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ScreenDefinition](
        [ID] [int] IDENTITY(1,1) NOT NULL,
        [AppID] [int] NOT NULL,
        [Code] [nvarchar](100) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [Route] [nvarchar](200) NULL,
        [IsDeleted] [bit] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime] NOT NULL,
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime] NULL,
     CONSTRAINT [PK_ScreenDefinition] PRIMARY KEY CLUSTERED ([ID] ASC),
     CONSTRAINT [FK_ScreenDefinition_AppDefinition] FOREIGN KEY ([AppID]) REFERENCES [dbo].[AppDefinition]([ID])
    ) ON [PRIMARY]
END
GO

IF OBJECT_ID('dbo.ProfileDefinition', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProfileDefinition](
        [ID] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [IsAdmin] [bit] NOT NULL,
        [IsDeleted] [bit] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime] NOT NULL,
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime] NULL,
     CONSTRAINT [PK_ProfileDefinition] PRIMARY KEY CLUSTERED ([ID] ASC)
    ) ON [PRIMARY]
END
GO

IF OBJECT_ID('dbo.PermissionDefinition', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PermissionDefinition](
        [ID] [int] IDENTITY(1,1) NOT NULL,
        [ScreenID] [int] NULL,
        [Action] [nvarchar](100) NULL,
        [PermissionKey] [nvarchar](150) NULL,
        [Code] [nvarchar](100) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [ApplicationID] [int] NULL,
        [IsDeleted] [bit] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime] NOT NULL,
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime] NULL,
     CONSTRAINT [PK_PermissionDefinition] PRIMARY KEY CLUSTERED ([ID] ASC),
     CONSTRAINT [FK_PermissionDefinition_ScreenDefinition] FOREIGN KEY ([ScreenID]) REFERENCES [dbo].[ScreenDefinition]([ID]),
     CONSTRAINT [FK_PermissionDefinition_AppDefinition] FOREIGN KEY ([ApplicationID]) REFERENCES [dbo].[AppDefinition]([ID])
    ) ON [PRIMARY]

    CREATE UNIQUE INDEX [UX_PermissionDefinition_Code] ON [dbo].[PermissionDefinition]([Code])
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_PermissionDefinition_Key'
      AND object_id = OBJECT_ID('dbo.PermissionDefinition')
)
BEGIN
    CREATE UNIQUE INDEX [UX_PermissionDefinition_Key]
    ON [dbo].[PermissionDefinition]([PermissionKey])
    WHERE [PermissionKey] IS NOT NULL;
END
GO

IF COL_LENGTH('dbo.PermissionDefinition', 'ScreenID') IS NULL
BEGIN
    ALTER TABLE [dbo].[PermissionDefinition]
    ADD [ScreenID] [int] NULL;
END
GO

IF COL_LENGTH('dbo.PermissionDefinition', 'Action') IS NULL
BEGIN
    ALTER TABLE [dbo].[PermissionDefinition]
    ADD [Action] [nvarchar](100) NULL;
END
GO

IF COL_LENGTH('dbo.PermissionDefinition', 'PermissionKey') IS NULL
BEGIN
    ALTER TABLE [dbo].[PermissionDefinition]
    ADD [PermissionKey] [nvarchar](150) NULL;
END
GO

IF COL_LENGTH('dbo.PermissionDefinition', 'ApplicationID') IS NULL
BEGIN
    ALTER TABLE [dbo].[PermissionDefinition]
    ADD [ApplicationID] [int] NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_PermissionDefinition_ScreenDefinition'
)
BEGIN
    ALTER TABLE [dbo].[PermissionDefinition]
    ADD CONSTRAINT [FK_PermissionDefinition_ScreenDefinition]
    FOREIGN KEY ([ScreenID]) REFERENCES [dbo].[ScreenDefinition]([ID]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_PermissionDefinition_AppDefinition'
)
BEGIN
    ALTER TABLE [dbo].[PermissionDefinition]
    ADD CONSTRAINT [FK_PermissionDefinition_AppDefinition]
    FOREIGN KEY ([ApplicationID]) REFERENCES [dbo].[AppDefinition]([ID]);
END
GO

IF OBJECT_ID('dbo.ProfilePermission', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProfilePermission](
        [ID] [int] IDENTITY(1,1) NOT NULL,
        [ProfileID] [int] NOT NULL,
        [PermissionID] [int] NOT NULL,
        [CanAdd] [bit] NOT NULL,
        [CanModify] [bit] NOT NULL,
        [CanDelete] [bit] NOT NULL,
        [CanSee] [bit] NOT NULL,
        [HasAccessToMenu] [bit] NOT NULL,
        [HasAccessToApp] [bit] NOT NULL,
        [IsDeleted] [bit] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime] NOT NULL,
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime] NULL,
     CONSTRAINT [PK_ProfilePermission] PRIMARY KEY CLUSTERED ([ID] ASC),
     CONSTRAINT [FK_ProfilePermission_ProfileDefinition] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[ProfileDefinition]([ID]),
     CONSTRAINT [FK_ProfilePermission_PermissionDefinition] FOREIGN KEY ([PermissionID]) REFERENCES [dbo].[PermissionDefinition]([ID])
    ) ON [PRIMARY]
END
GO

IF COL_LENGTH('dbo.ProfilePermission', 'HasAccessToMenu') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProfilePermission]
    ADD [HasAccessToMenu] [bit] NOT NULL CONSTRAINT [DF_ProfilePermission_HasAccessToMenu] DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.ProfilePermission', 'HasAccessToApp') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProfilePermission]
    ADD [HasAccessToApp] [bit] NOT NULL CONSTRAINT [DF_ProfilePermission_HasAccessToApp] DEFAULT (0);
END
GO

IF OBJECT_ID('dbo.UserDefinition', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserDefinition](
        [ID] [int] IDENTITY(1,1) NOT NULL,
        [ProfileID] [int] NOT NULL,
        [Username] [nvarchar](100) NOT NULL,
        [FullName] [nvarchar](150) NOT NULL,
        [Email] [nvarchar](150) NULL,
        [Password] [nvarchar](150) NOT NULL,
        [IsActive] [bit] NOT NULL,
        [IsDeleted] [bit] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime] NOT NULL,
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime] NULL,
     CONSTRAINT [PK_UserDefinition] PRIMARY KEY CLUSTERED ([ID] ASC),
     CONSTRAINT [FK_UserDefinition_ProfileDefinition] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[ProfileDefinition]([ID])
    ) ON [PRIMARY]

    CREATE UNIQUE INDEX [UX_UserDefinition_Username] ON [dbo].[UserDefinition]([Username])
END
GO

IF COL_LENGTH('dbo.UserDefinition', 'Password') IS NULL
BEGIN
    ALTER TABLE [dbo].[UserDefinition]
    ADD [Password] [nvarchar](150) NOT NULL CONSTRAINT [DF_UserDefinition_Password] DEFAULT ('');
END
GO

IF COL_LENGTH('dbo.UserDefinition', 'ProfileID') IS NULL
BEGIN
    ALTER TABLE [dbo].[UserDefinition]
    ADD [ProfileID] [int] NULL;
END
GO

IF COL_LENGTH('dbo.UserDefinition', 'DepartmentID') IS NULL
BEGIN
    ALTER TABLE [dbo].[UserDefinition]
    ADD [DepartmentID] [int] NULL;
END
GO

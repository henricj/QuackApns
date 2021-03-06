USE [master]
GO
/****** Object:  Database [ApnsClient]    Script Date: 11/30/2014 10:21:25 PM ******/
CREATE DATABASE [ApnsClient]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'ApnsClient', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\ApnsClient.mdf' , SIZE = 365568KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB ), 
 FILEGROUP [ApnsMod] CONTAINS MEMORY_OPTIMIZED_DATA  DEFAULT
( NAME = N'ApnsClient_Mod', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\ApnsClient_Mod' , MAXSIZE = UNLIMITED)
 LOG ON 
( NAME = N'ApnsClient_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\ApnsClient_log.ldf' , SIZE = 1341184KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [ApnsClient] SET COMPATIBILITY_LEVEL = 120
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [ApnsClient].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [ApnsClient] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [ApnsClient] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [ApnsClient] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [ApnsClient] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [ApnsClient] SET ARITHABORT OFF 
GO
ALTER DATABASE [ApnsClient] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [ApnsClient] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [ApnsClient] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [ApnsClient] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [ApnsClient] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [ApnsClient] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [ApnsClient] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [ApnsClient] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [ApnsClient] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [ApnsClient] SET  DISABLE_BROKER 
GO
ALTER DATABASE [ApnsClient] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [ApnsClient] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [ApnsClient] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [ApnsClient] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO
ALTER DATABASE [ApnsClient] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [ApnsClient] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [ApnsClient] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [ApnsClient] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [ApnsClient] SET  MULTI_USER 
GO
ALTER DATABASE [ApnsClient] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [ApnsClient] SET DB_CHAINING OFF 
GO
ALTER DATABASE [ApnsClient] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [ApnsClient] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [ApnsClient] SET DELAYED_DURABILITY = DISABLED 
GO
EXEC sys.sp_db_vardecimal_storage_format N'ApnsClient', N'ON'
GO
USE [ApnsClient]
GO
/****** Object:  Schema [Quack]    Script Date: 11/30/2014 10:21:25 PM ******/
CREATE SCHEMA [Quack]
GO
/****** Object:  UserDefinedTableType [Quack].[DeviceRegistrationTable]    Script Date: 11/30/2014 10:21:25 PM ******/
CREATE TYPE [Quack].[DeviceRegistrationTable] AS TABLE(
	[DeviceRegistrationId] [int] NOT NULL,
	[Token] [binary](32) NOT NULL,
	[UnixTimestamp] [int] NOT NULL,
	[ReceivedUtc] [datetime2](2) NOT NULL,
	INDEX [IX_Token] NONCLUSTERED HASH 
(
	[Token]
)WITH ( BUCKET_COUNT = 65536),
	 PRIMARY KEY NONCLUSTERED 
(
	[DeviceRegistrationId] ASC
)
)
WITH ( MEMORY_OPTIMIZED = ON )
GO
/****** Object:  UserDefinedTableType [Quack].[IntTable]    Script Date: 11/30/2014 10:21:25 PM ******/
CREATE TYPE [Quack].[IntTable] AS TABLE(
	[Id] [int] NOT NULL,
	 PRIMARY KEY NONCLUSTERED 
(
	[Id] ASC
)
)
WITH ( MEMORY_OPTIMIZED = ON )
GO
/****** Object:  UserDefinedTableType [Quack].[RegistrationTable]    Script Date: 11/30/2014 10:21:25 PM ******/
CREATE TYPE [Quack].[RegistrationTable] AS TABLE(
	[Token] [binary](32) NOT NULL,
	[UnixTimestamp] [int] NOT NULL,
	[ReceivedUtc] [datetime2](2) NOT NULL,
	 PRIMARY KEY NONCLUSTERED HASH 
(
	[Token]
)WITH ( BUCKET_COUNT = 65536)
)
WITH ( MEMORY_OPTIMIZED = ON )
GO
/****** Object:  Table [Quack].[Device]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Quack].[Device](
	[DeviceId] [int] IDENTITY(1,1) NOT NULL,
	[Token] [binary](32) NOT NULL,
	[UnixTimestamp] [int] NOT NULL,
	[CreatedUtc] [datetime2](2) NOT NULL CONSTRAINT [DF_Devices_CreatedUtc]  DEFAULT (sysutcdatetime()),
	[ModifiedUtc] [datetime2](2) NOT NULL CONSTRAINT [DF_Devices_ModifiedUtc]  DEFAULT (sysutcdatetime()),
	[ReceivedUtc] [datetime2](2) NOT NULL,
	[IsRegistered] [bit] NOT NULL CONSTRAINT [DF_Devices_IsRegistered]  DEFAULT ((1)),
	[TokenHash]  AS (CONVERT([smallint],hashbytes('SHA1',[Token]))) PERSISTED NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [Quack].[DeviceNotification]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[DeviceNotification](
	[NotificationSessionId] [int] NOT NULL,
	[DeviceId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [Quack].[DeviceRegistration]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Quack].[DeviceRegistration]
(
	[DeviceRegistrationId] [int] IDENTITY(1,1) NOT NULL,
	[Token] [binary](32) NOT NULL,
	[UnixTimestamp] [int] NOT NULL,
	[ReceivedUtc] [datetime2](2) NOT NULL,

CONSTRAINT [DeviceRegistrations_primaryKey] PRIMARY KEY NONCLUSTERED 
(
	[DeviceRegistrationId] ASC
)
)WITH ( MEMORY_OPTIMIZED = ON , DURABILITY = SCHEMA_AND_DATA )

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [Quack].[Notification]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Quack].[Notification](
	[NotificationId] [int] IDENTITY(1,1) NOT NULL,
	[Expiration] [int] NOT NULL,
	[Priority] [tinyint] NOT NULL,
	[Payload] [varbinary](2048) NOT NULL,
 CONSTRAINT [PK__Notifica__20CF2E126BEEC083] PRIMARY KEY CLUSTERED 
(
	[NotificationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [Quack].[NotificationBatch]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationBatch](
	[NotificationSessionId] [int] NOT NULL,
	[NotificationBatchId] [int] NOT NULL,
	[StartedUtc] [datetime2](2) NULL,
	[CompletedUtc] [datetime2](2) NULL,
	[TokenHashLow] [smallint] NOT NULL,
	[TokenHashHigh] [smallint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC,
	[NotificationBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [Quack].[NotificationGroup]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationGroup](
	[NotificationGroupId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationGroupId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [Quack].[NotificationGroupDevice]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationGroupDevice](
	[NotificationGroupId] [int] NOT NULL,
	[DeviceId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationGroupId] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [Quack].[NotificationSession]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationSession](
	[NotificationSessionId] [int] IDENTITY(1,1) NOT NULL,
	[NotificationId] [int] NOT NULL,
	[RequestedUtc] [datetime2](2) NOT NULL DEFAULT (sysdatetime()),
	[CompletedUtc] [datetime2](2) NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [Quack].[NotificationSessionCompletedDevice]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationSessionCompletedDevice](
	[NotificationSessionId] [int] NOT NULL,
	[DeviceId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [Quack].[NotificationSessionDevice]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationSessionDevice](
	[NotificationSessionId] [int] NOT NULL,
	[DeviceId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [Quack].[NotificationSessionGroup]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationSessionGroup](
	[NotificationSessionId] [int] NOT NULL,
	[NotificationGroupId] [int] NULL,
	[Batches] [int] NOT NULL,
	[PartitionSize]  AS ((65536)/[Batches]),
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  UserDefinedFunction [Quack].[GetNums]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [Quack].[GetNums](@n AS BIGINT) RETURNS TABLE
AS
-- http://sqlmag.com/sql-server/virtual-auxiliary-table-numbers
RETURN
  WITH
  L0   AS(SELECT 1 AS c UNION ALL SELECT 1),
  L1   AS(SELECT 1 AS c FROM L0 AS A CROSS JOIN L0 AS B),
  L2   AS(SELECT 1 AS c FROM L1 AS A CROSS JOIN L1 AS B),
  L3   AS(SELECT 1 AS c FROM L2 AS A CROSS JOIN L2 AS B),
  L4   AS(SELECT 1 AS c FROM L3 AS A CROSS JOIN L3 AS B),
  L5   AS(SELECT 1 AS c FROM L4 AS A CROSS JOIN L4 AS B),
  Nums AS(SELECT ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) AS n FROM L5)
  SELECT TOP (@n) n FROM Nums ORDER BY n;

GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IDX_DevicesToken]    Script Date: 11/30/2014 10:21:25 PM ******/
CREATE NONCLUSTERED INDEX [IDX_DevicesToken] ON [Quack].[Device]
(
	[Token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF

GO
/****** Object:  Index [IX_Quack_Devices_TokenHash]    Script Date: 11/30/2014 10:21:25 PM ******/
CREATE NONCLUSTERED INDEX [IX_Quack_Devices_TokenHash] ON [Quack].[Device]
(
	[TokenHash] ASC
)
WHERE ([IsRegistered]<>(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_Name]    Script Date: 11/30/2014 10:21:25 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Name] ON [Quack].[NotificationGroup]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [Quack].[DeviceNotification]  WITH CHECK ADD FOREIGN KEY([DeviceId])
REFERENCES [Quack].[Device] ([DeviceId])
GO
ALTER TABLE [Quack].[DeviceNotification]  WITH CHECK ADD FOREIGN KEY([NotificationSessionId])
REFERENCES [Quack].[NotificationSession] ([NotificationSessionId])
GO
ALTER TABLE [Quack].[NotificationBatch]  WITH CHECK ADD FOREIGN KEY([NotificationSessionId])
REFERENCES [Quack].[NotificationSession] ([NotificationSessionId])
GO
ALTER TABLE [Quack].[NotificationGroupDevice]  WITH CHECK ADD  CONSTRAINT [FK_Devices] FOREIGN KEY([DeviceId])
REFERENCES [Quack].[Device] ([DeviceId])
GO
ALTER TABLE [Quack].[NotificationGroupDevice] CHECK CONSTRAINT [FK_Devices]
GO
ALTER TABLE [Quack].[NotificationGroupDevice]  WITH CHECK ADD  CONSTRAINT [FK_NotificationGroup] FOREIGN KEY([NotificationGroupId])
REFERENCES [Quack].[NotificationGroup] ([NotificationGroupId])
GO
ALTER TABLE [Quack].[NotificationGroupDevice] CHECK CONSTRAINT [FK_NotificationGroup]
GO
ALTER TABLE [Quack].[NotificationSession]  WITH CHECK ADD  CONSTRAINT [FK__Notificat__Notif__13F1F5EB] FOREIGN KEY([NotificationId])
REFERENCES [Quack].[Notification] ([NotificationId])
GO
ALTER TABLE [Quack].[NotificationSession] CHECK CONSTRAINT [FK__Notificat__Notif__13F1F5EB]
GO
ALTER TABLE [Quack].[NotificationSessionCompletedDevice]  WITH CHECK ADD FOREIGN KEY([DeviceId])
REFERENCES [Quack].[Device] ([DeviceId])
GO
ALTER TABLE [Quack].[NotificationSessionCompletedDevice]  WITH CHECK ADD FOREIGN KEY([NotificationSessionId])
REFERENCES [Quack].[NotificationSession] ([NotificationSessionId])
GO
ALTER TABLE [Quack].[NotificationSessionDevice]  WITH CHECK ADD FOREIGN KEY([DeviceId])
REFERENCES [Quack].[NotificationGroup] ([NotificationGroupId])
GO
ALTER TABLE [Quack].[NotificationSessionDevice]  WITH CHECK ADD FOREIGN KEY([NotificationSessionId])
REFERENCES [Quack].[NotificationSession] ([NotificationSessionId])
GO
ALTER TABLE [Quack].[NotificationSessionGroup]  WITH CHECK ADD FOREIGN KEY([NotificationSessionId])
REFERENCES [Quack].[NotificationSession] ([NotificationSessionId])
GO
ALTER TABLE [Quack].[NotificationSessionGroup]  WITH CHECK ADD FOREIGN KEY([NotificationGroupId])
REFERENCES [Quack].[NotificationGroup] ([NotificationGroupId])
GO
/****** Object:  StoredProcedure [Quack].[AbortBatch]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Quack].[AbortBatch]
    @NotificationSessionId INT,
    @NotificationBatchId INT
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @NotificationSessionId IS NULL OR @NotificationBatchId IS NULL
    THROW 60000, N'NULL arguments', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE Quack.NotificationBatch
    SET StartedUtc = NULL
    WHERE NotificationSessionId = @NotificationSessionId AND NotificationBatchId = @NotificationBatchId
        AND StartedUtc IS NOT NULL;

    IF @@ROWCOUNT <> 1
        THROW 50000, N'No such batch is active', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
END CATCH;

GO
/****** Object:  StoredProcedure [Quack].[ApplyRegistrations]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Quack].[ApplyRegistrations]
    @BatchSize INT = 25000,
    @MaxRetries INT = 10
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Registrations Quack.RegistrationTable;
DECLARE @Subset Quack.IntTable;

IF @@TRANCOUNT > 0
    THROW 60000, N'This should probaby not be in a transaction', 1;

DECLARE @Retry INT = @MaxRetries

WHILE 1 = 1
BEGIN
    DELETE FROM @Subset;
    DELETE FROM @Registrations;

    BEGIN TRY

    BEGIN TRANSACTION;

    -- Get a sane number of rows to process.
    INSERT INTO @Subset (Id)
    SELECT TOP(@BatchSize) DR.DeviceRegistrationId
    FROM [Quack].[DeviceRegistration] DR WITH (SNAPSHOT)
    ORDER BY DR.DeviceRegistrationId;

    IF @@ROWCOUNT < 1
    BEGIN
        ROLLBACK TRANSACTION;
        BREAK;
    END;

    -- Get the latest information for each unique token.
    -- We make sure the tokens are unique by including
    -- the surrogate key in the ORDER BY clause.
    WITH RankedDr AS (
        SELECT DR.DeviceRegistrationId, DR.Token, DR.UnixTimestamp, DR.ReceivedUtc,
            ROW_NUMBER() OVER (
                PARTITION BY DR.[Token]
                ORDER BY DR.UnixTimestamp DESC, ReceivedUtc DESC, DeviceRegistrationId DESC
            ) AS RowNumber
        FROM Quack.DeviceRegistration DR WITH(SNAPSHOT)
        JOIN @Subset S ON S.Id = DR.DeviceRegistrationId
    )
    INSERT INTO @Registrations (Token, UnixTimestamp, ReceivedUtc)
    SELECT DR.Token, DR.UnixTimestamp, DR.ReceivedUtc
    FROM RankedDr DR
    WHERE DR.RowNumber = 1;

    DELETE
    FROM Quack.DeviceRegistration WITH (SNAPSHOT)
    WHERE DeviceRegistrationId IN (SELECT S.Id FROM @Subset S);

    DELETE FROM @Subset;

    MERGE Quack.Device AS T
    USING (
        SELECT R.Token, R.UnixTimestamp, R.ReceivedUtc
        FROM @Registrations R
    ) AS S
    ON T.Token = S.Token
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Token, UnixTimestamp, ReceivedUtc)
        VALUES (S.Token, S.UnixTimestamp, S.ReceivedUtc)
    WHEN MATCHED AND S.UnixTimestamp > T.UnixTimestamp THEN
        UPDATE SET T.UnixTimestamp = S.UnixTimestamp, T.IsRegistered = 1, T.ModifiedUtc = DEFAULT;

    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        SET @retry -= 1
  
        -- the error number for deadlocks (1205) does not need to be included for 
        -- transactions that do not access disk-based tables
        IF (@retry > 0 AND error_number() in (41302, 41305, 41325, 41301, 1205))
            WAITFOR DELAY '00:00:00.001'
        ELSE
            THROW;
    END CATCH;
END; -- WHILE


GO
/****** Object:  StoredProcedure [Quack].[ApplyRegistrationsTT]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Quack].[ApplyRegistrationsTT]
    @BatchSize INT = 25000,
    @MaxRetries INT = 10
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @@TRANCOUNT > 0
    THROW 60000, N'This should probaby not be in a transaction', 1;

DECLARE @Retry INT = @MaxRetries

CREATE TABLE #Registration 
(DeviceRegistrationId INT NOT NULL, Token BINARY(32) NOT NULL, UnixTimestamp INT NOT NULL, ReceivedUtc DATETIME2(2) NOT NULL);

WHILE 1 = 1
BEGIN
    TRUNCATE TABLE #Registration;

    BEGIN TRY

    BEGIN TRANSACTION;

    -- Get a sane number of rows to process.
    DELETE TOP(@BatchSize)
    FROM Quack.DeviceRegistration WITH(SNAPSHOT)
    OUTPUT DELETED.DeviceRegistrationId, DELETED.Token, DELETED.UnixTimestamp, DELETED.ReceivedUtc
    INTO #Registration (DeviceRegistrationId, Token, UnixTimestamp, ReceivedUtc);

    IF @@ROWCOUNT < 1
    BEGIN
        ROLLBACK TRANSACTION;
        BREAK;
    END;

    -- Get the latest information for each unique token.
    -- We make sure the tokens are unique by including
    -- the surrogate key in the ORDER BY clause.
    WITH RankedDr AS (
        SELECT DR.DeviceRegistrationId, DR.Token, DR.UnixTimestamp, DR.ReceivedUtc,
            ROW_NUMBER() OVER (
                PARTITION BY DR.[Token]
                ORDER BY DR.UnixTimestamp DESC, ReceivedUtc DESC, DeviceRegistrationId DESC
            ) AS RowNumber
        FROM #Registration DR
    )
    DELETE FROM RankedDr
    WHERE RankedDr.RowNumber > 1;

    MERGE Quack.Device AS T
    USING (
        SELECT R.Token, R.UnixTimestamp, R.ReceivedUtc
        FROM #Registration R
    ) AS S
    ON T.Token = S.Token
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Token, UnixTimestamp, ReceivedUtc)
        VALUES (S.Token, S.UnixTimestamp, S.ReceivedUtc)
    WHEN MATCHED AND S.UnixTimestamp > T.UnixTimestamp THEN
        UPDATE SET T.UnixTimestamp = S.UnixTimestamp, T.IsRegistered = 1, T.ModifiedUtc = DEFAULT;

    TRUNCATE TABLE #Registrations;

    COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        SET @retry -= 1
  
        -- the error number for deadlocks (1205) does not need to be included for 
        -- transactions that do not access disk-based tables
        IF (@retry > 0 AND error_number() in (41302, 41305, 41325, 41301, 1205))
            WAITFOR DELAY '00:00:00.001'
        ELSE
            THROW;
    END CATCH;
END; -- WHILE

DROP TABLE #Registration;


GO
/****** Object:  StoredProcedure [Quack].[ApplyRegistrationsTV]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Quack].[ApplyRegistrationsTV]
    @BatchSize INT = 25000,
    @MaxRetries INT = 10
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @@TRANCOUNT > 0
    THROW 60000, N'This should probaby not be in a transaction', 1;

DECLARE @Retry INT = @MaxRetries
--DECLARE @Iterations INT = 0;

DECLARE @Registration Quack.DeviceRegistrationTable;

WHILE 1 = 1
BEGIN
    DELETE FROM @Registration;

    BEGIN TRY

    BEGIN TRANSACTION;

    -- Get a sane number of rows to process.
    DELETE TOP(@BatchSize)
    FROM Quack.DeviceRegistration WITH(SNAPSHOT)
    OUTPUT DELETED.DeviceRegistrationId, DELETED.Token, DELETED.UnixTimestamp, DELETED.ReceivedUtc
    INTO @Registration (DeviceRegistrationId, Token, UnixTimestamp, ReceivedUtc);

    IF @@ROWCOUNT < 1
    BEGIN
        ROLLBACK TRANSACTION;
        BREAK;
    END;

    -- Get the latest information for each unique token.
    -- We make sure the tokens are unique by including
    -- the surrogate key in the ORDER BY clause.
    WITH RankedDr AS (
        SELECT DR.DeviceRegistrationId, DR.Token, DR.UnixTimestamp, DR.ReceivedUtc,
            ROW_NUMBER() OVER (
                PARTITION BY DR.[Token]
                ORDER BY DR.UnixTimestamp DESC, ReceivedUtc DESC, DeviceRegistrationId DESC
            ) AS RowNumber
        FROM @Registration DR
    )
    DELETE FROM RankedDr
    WHERE RankedDr.RowNumber > 1
    OPTION (RECOMPILE);

    MERGE Quack.Device AS T
    USING (
        SELECT R.Token, R.UnixTimestamp, R.ReceivedUtc
        FROM @Registration R
    ) AS S
    ON T.Token = S.Token
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Token, UnixTimestamp, ReceivedUtc)
        VALUES (S.Token, S.UnixTimestamp, S.ReceivedUtc)
    WHEN MATCHED AND S.UnixTimestamp > T.UnixTimestamp THEN
        UPDATE SET T.UnixTimestamp = S.UnixTimestamp, T.IsRegistered = 1, T.ReceivedUtc = S.ReceivedUtc, T.ModifiedUtc = DEFAULT
    ;--OPTION (RECOMPILE);

    COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        SET @retry -= 1;
  
        -- the error number for deadlocks (1205) does not need to be included for 
        -- transactions that do not access disk-based tables
        IF (@retry > 0 AND error_number() in (41302, 41305, 41325, 41301, 1205))
            WAITFOR DELAY '00:00:00.001'
        ELSE
            THROW;
    END CATCH;

    --SET @Iterations += 1;

    --IF 15 = @Iterations % 16
    --    WAITFOR DELAY '00:00:02';
END; -- WHILE

DELETE FROM @Registration;


GO
/****** Object:  StoredProcedure [Quack].[CompleteBatch]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Quack].[CompleteBatch]
    @NotificationSessionId INT,
    @NotificationBatchId INT
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @NotificationSessionId IS NULL OR @NotificationBatchId IS NULL
    THROW 60000, N'NULL arguments', 1;

DECLARE @Now DATETIME2(2) = SYSUTCDATETIME();

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE Quack.NotificationBatch
    SET CompletedUtc = @Now
    WHERE NotificationSessionId = @NotificationSessionId AND NotificationBatchId = @NotificationBatchId
        AND CompletedUtc IS NULL AND StartedUtc IS NOT NULL;

    IF @@ROWCOUNT <> 1
        THROW 50000, N'No such batch can be completed', 1;

    IF EXISTS(SELECT 1 FROM Quack.NotificationBatch NB WHERE NB.NotificationSessionId = @NotificationSessionId AND NB.CompletedUtc IS NULL)
        WITH BatchDevices AS (
            SELECT D.DeviceId
            FROM Quack.Device D
            CROSS JOIN Quack.NotificationBatch NB
            WHERE NB.NotificationSessionId = @NotificationSessionId
                AND D.TokenHash >= NB.TokenHashLow AND D.TokenHash <= NB.TokenHashHigh
                AND 0 <> D.IsRegistered
                AND NB.CompletedUtc IS NULL
        )
        DELETE FROM Quack.NotificationSessionCompletedDevice
        WHERE NotificationSessionId = @NotificationSessionId
            AND DeviceId NOT IN (SELECT BD.DeviceId FROM BatchDevices BD);
    ELSE
    BEGIN
        -- The whole session is done
        UPDATE Quack.NotificationSession
        SET CompletedUtc = @Now
        WHERE NotificationSessionId = @NotificationSessionId AND CompletedUtc IS NULL;

        IF @@ROWCOUNT <> 1
            THROW 50000, N'No such session can be completed', 1;

        DELETE FROM Quack.NotificationSessionCompletedDevice
        WHERE NotificationSessionId = @NotificationSessionId;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
END CATCH;

GO
/****** Object:  StoredProcedure [Quack].[CompletePartialBatch]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Quack].[CompletePartialBatch]
    @NotificationSessionId INT,
    @NotificationBatchId INT,
    @Devices Quack.IntTable READONLY
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @NotificationSessionId IS NULL OR @NotificationBatchId IS NULL
    THROW 60000, N'NULL arguments', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    EXEC Quack.ReportPartialBatch @NotificationSessionId = @NotificationSessionId,
        @NotificationBatchId = @NotificationBatchId,
        @Devices = @Devices;
    
    EXEC Quack.AbortBatch @NotificationSessionId = @NotificationSessionId,
         @NotificationBatchId = @NotificationBatchId; 

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
END CATCH;

GO
/****** Object:  StoredProcedure [Quack].[GetBatch]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Quack].[GetBatch]
    @BatchTimeout DATETIME2(2)
AS
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @BatchTimeout IS NULL
    THROW 60000, N'NULL Argument', 1;

DECLARE @Device AS Quack.IntTable;

DECLARE @NotificationSessionId INT, @NotificationBatchId INT;

DECLARE @Now DATETIME2(2) = SYSUTCDATETIME();

WHILE 1 = 1
BEGIN
    -- Get the next available batch...
    SELECT TOP(1) @NotificationSessionId = NS.NotificationSessionId,
        @NotificationBatchId = NB.NotificationBatchId
    FROM Quack.NotificationSession NS
    JOIN Quack.NotificationBatch NB ON NB.NotificationSessionId = NS.NotificationSessionId
    WHERE NS.CompletedUtc IS NULL
        AND NB.CompletedUtc IS NULL AND (NB.StartedUtc IS NULL OR NB.StartedUtc < @BatchTimeout)
    ORDER BY NS.NotificationSessionId, NB.NotificationBatchId;

    IF @NotificationSessionId IS NULL
        BREAK;

    -- Get all the devices in the batch
    INSERT INTO @Device (Id)
    SELECT D.DeviceId
    FROM Quack.Device D
    CROSS JOIN Quack.NotificationBatch NB
    WHERE D.TokenHash >= NB.TokenHashLow AND D.TokenHash <= NB.TokenHashHigh
        AND 0 <> D.IsRegistered
        AND NB.NotificationSessionId = @NotificationSessionId AND NB.NotificationBatchId = @NotificationBatchId
        AND (
            D.DeviceId IN (
                SELECT NGD.DeviceId
                FROM Quack.NotificationSessionGroup NSG
                JOIN Quack.NotificationGroupDevice NGD ON NGD.NotificationGroupId = NSG.NotificationGroupId
                WHERE NSG.NotificationSessionId = @NotificationSessionId
                UNION
                SELECT NSD.DeviceId
                FROM Quack.NotificationSessionDevice NSD
                WHERE NSD.NotificationSessionId = @NotificationSessionId
            )
            -- Or get everything if this is a broadcast
            OR EXISTS (
                SELECT 1
                FROM Quack.NotificationSessionGroup NSG
                WHERE NSG.NotificationSessionId = @NotificationSessionId AND NSG.NotificationGroupId IS NULL
            )
        )
        AND D.DeviceId NOT IN (
            SELECT NSCD.DeviceId
            FROM Quack.NotificationSessionCompletedDevice NSCD
            WHERE NSCD.NotificationSessionId = @NotificationSessionId
        );

    IF @@ROWCOUNT < 1
    BEGIN
        -- End this batch; it is empty.    
        EXEC Quack.CompleteBatch @NotificationSessionId, @NotificationBatchId;

        CONTINUE;
    END;

    UPDATE Quack.NotificationBatch
    SET StartedUtc = @Now
    WHERE NotificationSessionId = @NotificationSessionId AND NotificationBatchId = @NotificationBatchId;

    BREAK;
END; -- WHILE

SELECT @NotificationSessionId, @NotificationBatchId, D.Id [DeviceId]
FROM @Device D;

GO
/****** Object:  StoredProcedure [Quack].[GetNotifications]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Quack].[GetNotifications]
    @BatchTimeout DATETIME2(2),
    @BatchSize INT = 2500
AS
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @BatchSize IS NULL OR @BatchTimeout IS NULL
    THROW 60000, N'NULL Argument', 1;

CREATE TABLE #Notification (
    NotificationSessionId INT NOT NULL,
    DeviceId INT NOT NULL,
    NotificationBatchId INT NOT NULL,
    PRIMARY KEY (NotificationSessionId, DeviceId)
);

DECLARE @Count INT = 0;
DECLARE @Rowcount INT;

BEGIN TRY
    BEGIN TRANSACTION;

    WHILE @Count < @BatchSize
    BEGIN
        INSERT INTO #Notification (NotificationSessionId, NotificationBatchId, DeviceId)
        EXEC Quack.GetBatch @BatchTimeout;

        SET @Rowcount = @@ROWCOUNT;

        IF @Rowcount < 1
            BREAK;

        SET @Count += @Rowcount;
    END;

    COMMIT TRANSACTION;

    SELECT N.NotificationId, N.Expiration, N.[Priority], N.Payload
    FROM  Quack.[Notification] N
    WHERE N.NotificationId IN (
        SELECT NS.NotificationId
        FROM #Notification TN
        JOIN Quack.NotificationSession NS ON NS.NotificationSessionId = TN.NotificationSessionId
    );

    -- Is there some sane way to avoid the Quack.Device table scan that fetches Token?
    SELECT NS.NotificationSessionId, NS.NotificationId, TN.NotificationBatchId, TN.DeviceId, D.Token
    FROM #Notification TN
    JOIN Quack.Device D ON  D.DeviceId = TN.DeviceId
    JOIN Quack.NotificationSession NS ON NS.NotificationSessionId = TN.NotificationSessionId;

    DROP TABLE #Notification;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

GO
/****** Object:  StoredProcedure [Quack].[ReportPartialBatch]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Quack].[ReportPartialBatch]
    @NotificationSessionId INT,
    @NotificationBatchId INT,
    @Devices Quack.IntTable READONLY
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @NotificationSessionId IS NULL OR @NotificationBatchId IS NULL
    THROW 60000, N'NULL arguments', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO Quack.NotificationSessionCompletedDevice (NotificationSessionId, DeviceId)
    SELECT @NotificationSessionId, D.Id
    FROM @Devices D;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
END CATCH;

GO
/****** Object:  StoredProcedure [Quack].[SendBroadcastNotification]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Quack].[SendBroadcastNotification]
    @NotificationId INT,
    @BatchSize INT = 5000
AS
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @NotificationId IS NULL
    THROW 60000, N'NULL Argument', 1;

IF @BatchSize IS NULL OR @BatchSize < 1
    THROW 60000, N'Invalid batch size', 1;

DECLARE @EmptyTable Quack.IntTable;

EXEC Quack.SendNotification @NotificationId = @NotificationId,
    @Broadcast = 1,
    @NotificationGroups = @EmptyTable,
    @Devices = @EmptyTable,
    @BatchSize = @BatchSize;

GO
/****** Object:  StoredProcedure [Quack].[SendGroupNotification]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Quack].[SendGroupNotification]
    @NotificationId INT,
    @NotificationGroupId INT,
    @BatchSize INT = 5000
AS
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @NotificationId IS NULL OR @NotificationGroupId IS NULL
    THROW 60000, N'NULL Argument', 1;

IF @BatchSize IS NULL OR @BatchSize < 1
    THROW 60000, N'Invalid batch size', 1;

DECLARE @EmptyTable AS Quack.IntTable;

DECLARE @GroupTable AS Quack.IntTable;

INSERT INTO @GroupTable(Id)
VALUES  (@NotificationGroupId);

EXEC Quack.SendNotification @NotificationId = @NotificationId,
    @Broadcast = 0,
    @NotificationGroups = @GroupTable,
    @Devices = @EmptyTable,
    @BatchSize = @BatchSize;

GO
/****** Object:  StoredProcedure [Quack].[SendNotification]    Script Date: 11/30/2014 10:21:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Quack].[SendNotification]
    @NotificationId INT,
    @Broadcast BIT,
    @NotificationGroups Quack.IntTable READONLY,
    @Devices Quack.IntTable READONLY,
    @BatchSize INT = 5000
AS
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF @NotificationId IS NULL
    THROW 60000, N'NULL Argument', 1;

IF @BatchSize IS NULL OR @BatchSize < 1
    THROW 60000, N'Invalid batch size', 1;

DECLARE @Now DATETIME2(2) = SYSUTCDATETIME();

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @BatchCount INT;
    DECLARE @Batches INT;

    IF @Broadcast IS NOT NULL AND 0 <> @Broadcast
    BEGIN
        SET @BatchCount = (SELECT COUNT(*) FROM Quack.Device D WHERE 0 <> D.IsRegistered);
    END;
    ELSE
    BEGIN
        SET @BatchCount = (
            SELECT COUNT(*)
            FROM Quack.Device D
            WHERE 0 <> D.IsRegistered
                AND D.DeviceId IN (
                    SELECT NGD.DeviceId
                    FROM @NotificationGroups NSG
                    JOIN Quack.NotificationGroupDevice NGD ON NGD.NotificationGroupId = NSG.Id
                    UNION
                    SELECT TD.Id
                    FROM @Devices TD
                )
        );
    END;

    IF @BatchCount < 1
    BEGIN
        ROLLBACK TRANSACTION;

        SELECT NULL [NotificationSessionId], 0 [Devices];

        RETURN;
    END;

    INSERT INTO Quack.NotificationSession (NotificationId, RequestedUtc)
    VALUES (@NotificationId, @Now);

    DECLARE @NotificationSessionId INT = SCOPE_IDENTITY();
    SET @Batches = @BatchCount / @BatchSize;

    IF @Batches < 1
        SET @Batches = 1;

    DECLARE @BatchStep INT = (65536 + @Batches - 1) / @Batches;

    WITH Steps AS (
        SELECT NR.n,
        (NR.n - 1) * @BatchStep - 32768 [From],
        (NR.n - 1) * @BatchStep - 32768 + @BatchStep - 1 [To]
        FROM Quack.GetNums(@Batches) NR
    ),
    Bounded AS (
        SELECT S.n,
            CASE WHEN S.[From] < -32768 THEN -32768 ELSE S.[From] END [From],
            CASE WHEN S.[To] > 32767 THEN 32767 ELSE S.[To] END [To]
        FROM Steps S
        WHERE S.[From] <= 37267
    )
    INSERT INTO Quack.NotificationBatch (
        NotificationSessionId,
        NotificationBatchId,
        TokenHashLow,
        TokenHashHigh
    )
    SELECT @NotificationSessionId, B.n, B.[From], B.[To]
    FROM Bounded B
    WHERE B.[From] <= B.[To]

    DECLARE @ActualBatches INT, @MinFrom INT, @MaxTo INT;
    
    SELECT @ActualBatches = COUNT(*), @MinFrom = MIN(NB.TokenHashLow), @MaxTo = MAX(NB.TokenHashHigh)
    FROM Quack.NotificationBatch NB
    WHERE NB.NotificationSessionId = @NotificationSessionId

    IF @ActualBatches <> @Batches
    BEGIN
        PRINT 'Batch count mismatch';
        SET @Batches = @ActualBatches;
    END;

    IF @MinFrom <> -32768
        THROW 60001, N'Minimum From is invalid', 1;

    IF @MaxTo <> 32767
        THROW 60001, N'Maximum To is invalid', 1;

    IF @Broadcast IS NOT NULL AND 0 <> @Broadcast
        INSERT INTO Quack.NotificationSessionGroup (NotificationSessionId, NotificationGroupId, [Batches])
        VALUES  (@NotificationSessionId, NULL, @Batches);
    ELSE
    BEGIN
        INSERT INTO Quack.NotificationSessionGroup (NotificationSessionId, NotificationGroupId, [Batches])
        SELECT @NotificationSessionId, NG2.NotificationGroupId, @Batches
        FROM @NotificationGroups NG
        JOIN Quack.NotificationGroup NG2 ON NG.Id = NG2.NotificationGroupId;

        INSERT INTO Quack.NotificationSessionDevice (NotificationSessionId, DeviceId)
        SELECT @NotificationSessionId, D.DeviceId
        FROM Quack.Device D
        WHERE 0 <> D.IsRegistered
            AND D.DeviceId IN (
                SELECT D2.Id
                FROM @Devices D2
            );
    END;

    COMMIT TRANSACTION;

    SELECT @NotificationSessionId [NotificationSessionId], @BatchCount [Devices];
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;


GO
USE [master]
GO
ALTER DATABASE [ApnsClient] SET  READ_WRITE 
GO

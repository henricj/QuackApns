USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[SendGroupNotification]    Script Date: 11/30/2014 10:20:53 PM ******/
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

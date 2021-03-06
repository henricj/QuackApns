USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[SendBroadcastNotification]    Script Date: 11/30/2014 10:20:53 PM ******/
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

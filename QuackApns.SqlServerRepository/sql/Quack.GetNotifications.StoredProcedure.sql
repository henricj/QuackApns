USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[GetNotifications]    Script Date: 11/30/2014 10:20:53 PM ******/
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

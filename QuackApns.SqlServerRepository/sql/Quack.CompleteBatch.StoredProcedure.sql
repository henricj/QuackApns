USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[CompleteBatch]    Script Date: 11/30/2014 10:20:53 PM ******/
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

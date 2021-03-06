USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[GetBatch]    Script Date: 11/30/2014 10:20:53 PM ******/
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

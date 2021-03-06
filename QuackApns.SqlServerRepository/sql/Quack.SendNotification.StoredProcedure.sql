USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[SendNotification]    Script Date: 11/30/2014 10:20:53 PM ******/
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

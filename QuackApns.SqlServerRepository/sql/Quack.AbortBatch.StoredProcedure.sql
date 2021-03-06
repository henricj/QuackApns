USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[AbortBatch]    Script Date: 11/30/2014 10:20:53 PM ******/
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

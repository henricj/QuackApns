USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[ReportPartialBatch]    Script Date: 11/30/2014 10:20:53 PM ******/
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

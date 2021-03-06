USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[ApplyRegistrationsTT]    Script Date: 11/30/2014 10:20:53 PM ******/
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

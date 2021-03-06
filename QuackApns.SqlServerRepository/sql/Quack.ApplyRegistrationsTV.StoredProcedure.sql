USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[ApplyRegistrationsTV]    Script Date: 11/30/2014 10:20:53 PM ******/
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

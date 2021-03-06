USE [ApnsClient]
GO
/****** Object:  StoredProcedure [Quack].[ApplyRegistrations]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Quack].[ApplyRegistrations]
    @BatchSize INT = 25000,
    @MaxRetries INT = 10
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Registrations Quack.RegistrationTable;
DECLARE @Subset Quack.IntTable;

IF @@TRANCOUNT > 0
    THROW 60000, N'This should probaby not be in a transaction', 1;

DECLARE @Retry INT = @MaxRetries

WHILE 1 = 1
BEGIN
    DELETE FROM @Subset;
    DELETE FROM @Registrations;

    BEGIN TRY

    BEGIN TRANSACTION;

    -- Get a sane number of rows to process.
    INSERT INTO @Subset (Id)
    SELECT TOP(@BatchSize) DR.DeviceRegistrationId
    FROM [Quack].[DeviceRegistration] DR WITH (SNAPSHOT)
    ORDER BY DR.DeviceRegistrationId;

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
        FROM Quack.DeviceRegistration DR WITH(SNAPSHOT)
        JOIN @Subset S ON S.Id = DR.DeviceRegistrationId
    )
    INSERT INTO @Registrations (Token, UnixTimestamp, ReceivedUtc)
    SELECT DR.Token, DR.UnixTimestamp, DR.ReceivedUtc
    FROM RankedDr DR
    WHERE DR.RowNumber = 1;

    DELETE
    FROM Quack.DeviceRegistration WITH (SNAPSHOT)
    WHERE DeviceRegistrationId IN (SELECT S.Id FROM @Subset S);

    DELETE FROM @Subset;

    MERGE Quack.Device AS T
    USING (
        SELECT R.Token, R.UnixTimestamp, R.ReceivedUtc
        FROM @Registrations R
    ) AS S
    ON T.Token = S.Token
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Token, UnixTimestamp, ReceivedUtc)
        VALUES (S.Token, S.UnixTimestamp, S.ReceivedUtc)
    WHEN MATCHED AND S.UnixTimestamp > T.UnixTimestamp THEN
        UPDATE SET T.UnixTimestamp = S.UnixTimestamp, T.IsRegistered = 1, T.ModifiedUtc = DEFAULT;

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


GO

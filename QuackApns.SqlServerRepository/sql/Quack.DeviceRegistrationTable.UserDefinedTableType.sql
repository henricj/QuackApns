USE [ApnsClient]
GO
/****** Object:  UserDefinedTableType [Quack].[DeviceRegistrationTable]    Script Date: 11/30/2014 10:20:52 PM ******/
CREATE TYPE [Quack].[DeviceRegistrationTable] AS TABLE(
	[DeviceRegistrationId] [int] NOT NULL,
	[Token] [binary](32) NOT NULL,
	[UnixTimestamp] [int] NOT NULL,
	[ReceivedUtc] [datetime2](2) NOT NULL,
	INDEX [IX_Token] NONCLUSTERED HASH 
(
	[Token]
)WITH ( BUCKET_COUNT = 65536),
	 PRIMARY KEY NONCLUSTERED 
(
	[DeviceRegistrationId] ASC
)
)
WITH ( MEMORY_OPTIMIZED = ON )
GO

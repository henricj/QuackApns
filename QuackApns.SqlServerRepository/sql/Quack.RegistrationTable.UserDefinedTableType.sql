USE [ApnsClient]
GO
/****** Object:  UserDefinedTableType [Quack].[RegistrationTable]    Script Date: 11/30/2014 10:20:53 PM ******/
CREATE TYPE [Quack].[RegistrationTable] AS TABLE(
	[Token] [binary](32) NOT NULL,
	[UnixTimestamp] [int] NOT NULL,
	[ReceivedUtc] [datetime2](2) NOT NULL,
	 PRIMARY KEY NONCLUSTERED HASH 
(
	[Token]
)WITH ( BUCKET_COUNT = 65536)
)
WITH ( MEMORY_OPTIMIZED = ON )
GO

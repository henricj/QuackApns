USE [ApnsClient]
GO
/****** Object:  UserDefinedTableType [Quack].[IntTable]    Script Date: 11/30/2014 10:20:53 PM ******/
CREATE TYPE [Quack].[IntTable] AS TABLE(
	[Id] [int] NOT NULL,
	 PRIMARY KEY NONCLUSTERED 
(
	[Id] ASC
)
)
WITH ( MEMORY_OPTIMIZED = ON )
GO

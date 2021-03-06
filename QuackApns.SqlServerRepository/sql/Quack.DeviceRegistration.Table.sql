USE [ApnsClient]
GO
/****** Object:  Table [Quack].[DeviceRegistration]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Quack].[DeviceRegistration]
(
	[DeviceRegistrationId] [int] IDENTITY(1,1) NOT NULL,
	[Token] [binary](32) NOT NULL,
	[UnixTimestamp] [int] NOT NULL,
	[ReceivedUtc] [datetime2](2) NOT NULL,

CONSTRAINT [DeviceRegistrations_primaryKey] PRIMARY KEY NONCLUSTERED 
(
	[DeviceRegistrationId] ASC
)
)WITH ( MEMORY_OPTIMIZED = ON , DURABILITY = SCHEMA_AND_DATA )

GO
SET ANSI_PADDING OFF
GO

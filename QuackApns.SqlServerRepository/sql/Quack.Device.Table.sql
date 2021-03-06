USE [ApnsClient]
GO
/****** Object:  Table [Quack].[Device]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Quack].[Device](
	[DeviceId] [int] IDENTITY(1,1) NOT NULL,
	[Token] [binary](32) NOT NULL,
	[UnixTimestamp] [int] NOT NULL,
	[CreatedUtc] [datetime2](2) NOT NULL CONSTRAINT [DF_Devices_CreatedUtc]  DEFAULT (sysutcdatetime()),
	[ModifiedUtc] [datetime2](2) NOT NULL CONSTRAINT [DF_Devices_ModifiedUtc]  DEFAULT (sysutcdatetime()),
	[ReceivedUtc] [datetime2](2) NOT NULL,
	[IsRegistered] [bit] NOT NULL CONSTRAINT [DF_Devices_IsRegistered]  DEFAULT ((1)),
	[TokenHash]  AS (CONVERT([smallint],hashbytes('SHA1',[Token]))) PERSISTED NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IDX_DevicesToken]    Script Date: 11/30/2014 10:20:53 PM ******/
CREATE NONCLUSTERED INDEX [IDX_DevicesToken] ON [Quack].[Device]
(
	[Token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF

GO
/****** Object:  Index [IX_Quack_Devices_TokenHash]    Script Date: 11/30/2014 10:20:53 PM ******/
CREATE NONCLUSTERED INDEX [IX_Quack_Devices_TokenHash] ON [Quack].[Device]
(
	[TokenHash] ASC
)
WHERE ([IsRegistered]<>(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

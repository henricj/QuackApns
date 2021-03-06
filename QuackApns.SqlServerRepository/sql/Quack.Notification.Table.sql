USE [ApnsClient]
GO
/****** Object:  Table [Quack].[Notification]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Quack].[Notification](
	[NotificationId] [int] IDENTITY(1,1) NOT NULL,
	[Expiration] [int] NOT NULL,
	[Priority] [tinyint] NOT NULL,
	[Payload] [varbinary](2048) NOT NULL,
 CONSTRAINT [PK__Notifica__20CF2E126BEEC083] PRIMARY KEY CLUSTERED 
(
	[NotificationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO

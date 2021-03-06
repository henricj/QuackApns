USE [ApnsClient]
GO
/****** Object:  Table [Quack].[NotificationBatch]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationBatch](
	[NotificationSessionId] [int] NOT NULL,
	[NotificationBatchId] [int] NOT NULL,
	[StartedUtc] [datetime2](2) NULL,
	[CompletedUtc] [datetime2](2) NULL,
	[TokenHashLow] [smallint] NOT NULL,
	[TokenHashHigh] [smallint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC,
	[NotificationBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [Quack].[NotificationBatch]  WITH CHECK ADD FOREIGN KEY([NotificationSessionId])
REFERENCES [Quack].[NotificationSession] ([NotificationSessionId])
GO

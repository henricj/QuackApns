USE [ApnsClient]
GO
/****** Object:  Table [Quack].[NotificationSessionGroup]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationSessionGroup](
	[NotificationSessionId] [int] NOT NULL,
	[NotificationGroupId] [int] NULL,
	[Batches] [int] NOT NULL,
	[PartitionSize]  AS ((65536)/[Batches]),
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [Quack].[NotificationSessionGroup]  WITH CHECK ADD FOREIGN KEY([NotificationSessionId])
REFERENCES [Quack].[NotificationSession] ([NotificationSessionId])
GO
ALTER TABLE [Quack].[NotificationSessionGroup]  WITH CHECK ADD FOREIGN KEY([NotificationGroupId])
REFERENCES [Quack].[NotificationGroup] ([NotificationGroupId])
GO

USE [ApnsClient]
GO
/****** Object:  Table [Quack].[NotificationSessionDevice]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationSessionDevice](
	[NotificationSessionId] [int] NOT NULL,
	[DeviceId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [Quack].[NotificationSessionDevice]  WITH CHECK ADD FOREIGN KEY([DeviceId])
REFERENCES [Quack].[NotificationGroup] ([NotificationGroupId])
GO
ALTER TABLE [Quack].[NotificationSessionDevice]  WITH CHECK ADD FOREIGN KEY([NotificationSessionId])
REFERENCES [Quack].[NotificationSession] ([NotificationSessionId])
GO

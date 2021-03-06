USE [ApnsClient]
GO
/****** Object:  Table [Quack].[NotificationGroupDevice]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationGroupDevice](
	[NotificationGroupId] [int] NOT NULL,
	[DeviceId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationGroupId] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [Quack].[NotificationGroupDevice]  WITH CHECK ADD  CONSTRAINT [FK_Devices] FOREIGN KEY([DeviceId])
REFERENCES [Quack].[Device] ([DeviceId])
GO
ALTER TABLE [Quack].[NotificationGroupDevice] CHECK CONSTRAINT [FK_Devices]
GO
ALTER TABLE [Quack].[NotificationGroupDevice]  WITH CHECK ADD  CONSTRAINT [FK_NotificationGroup] FOREIGN KEY([NotificationGroupId])
REFERENCES [Quack].[NotificationGroup] ([NotificationGroupId])
GO
ALTER TABLE [Quack].[NotificationGroupDevice] CHECK CONSTRAINT [FK_NotificationGroup]
GO

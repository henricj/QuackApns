USE [ApnsClient]
GO
/****** Object:  Table [Quack].[NotificationSession]    Script Date: 11/30/2014 10:20:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Quack].[NotificationSession](
	[NotificationSessionId] [int] IDENTITY(1,1) NOT NULL,
	[NotificationId] [int] NOT NULL,
	[RequestedUtc] [datetime2](2) NOT NULL DEFAULT (sysdatetime()),
	[CompletedUtc] [datetime2](2) NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationSessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [Quack].[NotificationSession]  WITH CHECK ADD  CONSTRAINT [FK__Notificat__Notif__13F1F5EB] FOREIGN KEY([NotificationId])
REFERENCES [Quack].[Notification] ([NotificationId])
GO
ALTER TABLE [Quack].[NotificationSession] CHECK CONSTRAINT [FK__Notificat__Notif__13F1F5EB]
GO

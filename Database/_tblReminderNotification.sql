USE [DWREPORTS]
GO

/****** Object:  Table [dbo].[_tblReminderNotification]    Script Date: 7/9/2026 1:08:13 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[_tblReminderNotification](
	[PK_ReminderID] [int] IDENTITY(1,1) NOT NULL,
	[PermitID] [int] NOT NULL,
	[ReminderType] [varchar](30) NOT NULL,
	[RecipientEmail] [varchar](255) NOT NULL,
	[ContainerETA] [date] NULL,
	[EmailStatus] [varchar](20) NOT NULL,
	[SentDate] [datetime] NULL,
	[CreatedDate] [datetime] NOT NULL,
	[HBLNo] [varchar](100) NULL,
	[ContainerNo] [varchar](100) NULL,
	[PONo] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[PK_ReminderID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[_tblReminderNotification] ADD  DEFAULT ('Pending') FOR [EmailStatus]
GO

ALTER TABLE [dbo].[_tblReminderNotification] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO



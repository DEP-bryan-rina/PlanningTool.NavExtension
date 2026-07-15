USE [DWREPORTS]
GO

/****** Object:  Table [dbo].[_tblEmailNotification]    Script Date: 7/9/2026 1:07:01 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[_tblEmailNotification](
	[_PK_ptEmailNotification_ID] [int] IDENTITY(1,1) NOT NULL,
	[_ptEmailAddress] [varchar](255) NOT NULL,
	[_ptImpEx] [bit] NOT NULL,
	[_ptProcurement] [bit] NOT NULL,
	[_pt2WeeksReminder] [bit] NOT NULL,
	[_ptDelaysReminder] [bit] NOT NULL,
	[_ptUserType] [varchar](20) NOT NULL,
	[_ptStatus] [varchar](20) NOT NULL,
	[_ptCreatedDate] [datetime] NULL,
	[_ptUpdatedDate] [datetime] NULL,
 CONSTRAINT [PK__tblEmailNotification] PRIMARY KEY CLUSTERED 
(
	[_PK_ptEmailNotification_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[_tblEmailNotification] ADD  DEFAULT ((0)) FOR [_pt2WeeksReminder]
GO



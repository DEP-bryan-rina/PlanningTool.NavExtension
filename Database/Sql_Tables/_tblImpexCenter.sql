USE [DWREPORTS]
GO

/****** Object:  Table [dbo].[_tblImPexCenter]    Script Date: 7/9/2026 1:04:57 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[_tblImPexCenter](
	[PK_ptImpexID] [int] IDENTITY(1,1) NOT NULL,
	[_ptHBLNo] [nvarchar](50) NULL,
	[_ptContainerNo] [nvarchar](100) NULL,
	[_ptContainerETA] [date] NULL,
	[_ptCreatedBy] [nvarchar](100) NULL,
	[_ptCreatedDate] [datetime] NULL,
	[_ptModifiedBy] [nvarchar](100) NULL,
	[_ptModifiedDate] [datetime] NULL,
	[_ptRemarks] [nvarchar](255) NULL,
	[_ptStatus] [int] NULL,
	[_ptCountryOfOrigin] [varchar](50) NULL,
 CONSTRAINT [PK___tblImpe__3F0B144E8B90CB0A] PRIMARY KEY CLUSTERED 
(
	[PK_ptImpexID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

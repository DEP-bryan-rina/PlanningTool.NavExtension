USE [DWREPORTS]
GO

/****** Object:  Table [dbo].[_tblProcurementCenter]    Script Date: 7/9/2026 1:06:18 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[_tblProcurementCenter](
	[PK_ptProcID] [int] IDENTITY(1,1) NOT NULL,
	[_ptDocType] [int] NULL,
	[_ptDocNo] [nvarchar](50) NULL,
	[_ptLineNo] [int] NULL,
	[_ptHBLNo] [nvarchar](50) NULL,
	[_ptContainerNo] [nvarchar](50) NULL,
	[_ptVendorName] [nvarchar](255) NULL,
	[_ptItemCode] [nvarchar](100) NULL,
	[_ptItemDescription] [nvarchar](255) NULL,
	[_ptOutstandingQty] [decimal](18, 2) NULL,
	[_ptPartialDelivered] [decimal](18, 2) NULL,
	[_ptBalanceQty] [decimal](18, 2) NULL,
	[_ptContainerETA] [date] NULL,
	[_ptCreatedBy] [nvarchar](100) NULL,
	[_ptCreatedDate] [datetime] NULL,
	[_ptModifiedBy] [nvarchar](100) NULL,
	[_ptModifiedDate] [datetime] NULL,
	[_ptRemarks] [nvarchar](255) NULL,
	[_ptStatus] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[PK_ptProcID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO



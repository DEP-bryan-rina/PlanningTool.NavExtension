USE [DWREPORTS]
GO

/****** Object:  View [dbo].[vw_ProcurementCenterBalance]    Script Date: 7/30/2026 10:14:56 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER VIEW [dbo].[vw_ProcurementCenterBalance]
AS
SELECT        p.PK_ptProcID, p._ptDocNo, p._ptLineNo, p._ptHBLNo, p._ptContainerNo, p._ptVendorName, p._ptItemCode, p._ptItemDescription, p._ptOutstandingQty, p._ptPartialDelivered, 
                         p._ptOutstandingQty - ISNULL(SUM(CASE WHEN x._ptStatus = 1 THEN x._ptPartialDelivered ELSE 0 END), 0) AS _ptBalanceQty, p._ptCountryOfOrigin, p._ptContainerETA, p._ptRemarks, p._ptStatus
FROM            dbo._tblProcurementCenter AS p LEFT OUTER JOIN
                         dbo._tblProcurementCenter AS x ON x._ptDocNo = p._ptDocNo AND x._ptItemCode = p._ptItemCode
GROUP BY p.PK_ptProcID, p._ptDocNo, p._ptLineNo, p._ptHBLNo, p._ptContainerNo, p._ptVendorName, p._ptItemCode, p._ptItemDescription, p._ptOutstandingQty, p._ptPartialDelivered, p._ptCountryOfOrigin, p._ptContainerETA, 
                         p._ptRemarks, p._ptStatus
GO



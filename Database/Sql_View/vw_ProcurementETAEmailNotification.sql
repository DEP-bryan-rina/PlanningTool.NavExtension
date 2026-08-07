USE [DWREPORTS]
GO

/****** Object:  View [dbo].[vw_ProcurementETAEmailNotification]    Script Date: 8/7/2026 9:11:22 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER VIEW [dbo].[vw_ProcurementETAEmailNotification]
AS

WITH ShipmentStatus AS
(
    SELECT
        p.PK_ptProcID,
        p._ptDocNo,
        p._ptHBLNo,
        p._ptContainerNo,
        p._ptVendorName,
        p._ptItemCode,
        p._ptItemDescription,
        p._ptCountryOfOrigin,
        p._ptContainerETA,
        p._ptPartialDelivered,
        d.DaysToETA,

        CASE
            WHEN d.DaysToETA BETWEEN 1 AND 14 THEN 'Upcoming'
            WHEN d.DaysToETA <= -7 THEN 'Delayed'
        END AS ShipmentStatus

    FROM dbo._tblProcurementCenter p

    CROSS APPLY
    (
        SELECT DATEDIFF
        (
            DAY,
            CAST(GETDATE() AS DATE),
            p._ptContainerETA
        ) AS DaysToETA
    ) d

    WHERE
        p._ptStatus = 1
        AND p._ptContainerETA IS NOT NULL
)

SELECT
    s.*,
    e._PK_ptEmailNotification_ID,
    e._ptEmailAddress,
    h._ptLastSentDate

FROM ShipmentStatus s

INNER JOIN dbo._tblEmailNotification e
    ON e._ptStatus = 'Active'

OUTER APPLY
(
    SELECT TOP (1)
        sh._ptLastSentDate
    FROM dbo._tblShipmentEmailHistory sh
    WHERE
        sh._ptProcID = s.PK_ptProcID
        AND sh._ptEmailNotificationID = e._PK_ptEmailNotification_ID
    ORDER BY sh._ptLastSentDate DESC
) h

WHERE

(
    s.ShipmentStatus = 'Upcoming'
    AND e._pt2WeeksReminder = 1
    AND
    (
        h._ptLastSentDate IS NULL
        OR
        (
            e._pt2WeeksTrigger = 'Friday'
            AND DATENAME(WEEKDAY, GETDATE()) = 'Friday'
            AND CAST(h._ptLastSentDate AS DATE) <> CAST(GETDATE() AS DATE)
        )
    )
)

OR

(
    s.ShipmentStatus = 'Delayed'
    AND e._ptDelaysReminder = 1
    AND
    (
        h._ptLastSentDate IS NULL
        OR
        (
            e._ptDelaysTrigger = 'Daily'
            AND CAST(h._ptLastSentDate AS DATE) <> CAST(GETDATE() AS DATE)
        )
    )
);
GO



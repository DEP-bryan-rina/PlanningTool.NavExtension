using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Constants;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;

namespace PlanningTool.NavExtension.Services
{
    public class ProcurementReminderService
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;


        public ProcurementReminderService(
            AppDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task ProcessProcurementReminders()
        {
            var notifications = await GetNotificationsAsync();

            if (!notifications.Any())
                return;


            var statusGroups = notifications
                .GroupBy(x => x.ShipmentStatus);


            foreach (var group in statusGroups)
            {
                await SendBatchEmailAsync(group);
            }
        }

        private async Task<List<vw_ProcurementETAEmailNotification>>
            GetNotificationsAsync()
        {
            return await _context
                .vw_ProcurementETAEmailNotification
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task SendBatchEmailAsync(
            IGrouping<string, vw_ProcurementETAEmailNotification> shipmentGroup)
        {
            try
            {
                var recipients = shipmentGroup
                    .Select(x => x._ptEmailAddress)
                    .Distinct()
                    .ToList();


                if (!recipients.Any())
                    return;

                var subject =
                    GetSubject(shipmentGroup.Key);

                var body =
                    await BuildEmailBodyAsync(
                        shipmentGroup);

                await _emailService.SendEmailAsync(
                    recipients,
                    subject,
                    body,
                    EmailModules.ShipmentNotification);


                await SaveEmailHistoryAsync(
                    shipmentGroup);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string GetSubject(string status)
        {
            return status switch
            {
                "Upcoming" =>
                    "DEDON ETA | Upcoming Shipment Notification",

                "Shipment Today" =>
                    "DEDON ETA | Shipment Arriving Today Notification",

                "Due" =>
                    "DEDON ETA | Shipment Due Notification",

                "Delayed" =>
                    "DEDON ETA | Delayed Shipment Notification",

                _ =>
                    "DEDON Procurement Notification"
            };
        }


        private async Task<string> BuildEmailBodyAsync(
            IGrouping<string, vw_ProcurementETAEmailNotification> shipmentGroup)
        {

            var items = shipmentGroup
            .GroupBy(x => x.PK_ptProcID)
            .Select(g => g.First())
            .Select(x => new
            {
                x._ptDocNo,
                x._ptHBLNo,
                x._ptContainerNo,
                x._ptVendorName,
                x._ptItemDescription,
                x._ptCountryOfOrigin,
                x._ptContainerETA,
                x._ptPartialDelivered
            })
            .ToList();



            var rows = string.Join("", items.Select(x => $@"

                <tr>
                    <td style='padding:5px;text-align:left'>{x._ptDocNo}</td>
                    <td style='padding:5px;text-align:left'>{x._ptHBLNo}</td>
                    <td style='padding:5px;text-align:left'>{x._ptContainerNo}</td>
                    <td style='padding:5px;text-align:left'>{x._ptVendorName ?? "-"}</td>
                    <td style='padding:5px;text-align:left'>{x._ptItemDescription ?? "-"}</td>
                    <td style='padding:5px;text-align:left'>{x._ptCountryOfOrigin ?? "-"}</td>
                    <td style='padding:5px;text-align:left'>{x._ptContainerETA:yyyy-MM-dd}</td>
                    <td style='padding:5px;text-align:right'>
                        {x._ptPartialDelivered:N2}
                    </td>
                </tr>

                "));



            var etaList = string.Join(", ",
                shipmentGroup
                .Select(x => x._ptContainerETA)
                .Distinct()
                .Select(x => x?.ToString("yyyy-MM-dd")));



            var statusText = shipmentGroup.Key switch
            {
                "Upcoming" =>
                    "Upcoming shipment arrival",

                "Overdue" =>
                    "Shipment overdue",

                "Delayed" =>
                    "Shipment delayed",

                _ =>
                    shipmentGroup.Key
            };



            return $@"

                <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

                <p>Dear Team,</p>


                <p>
                    {GetShipmentMessage(shipmentGroup.Key)}
                </p>


                <table cellpadding='3'>

                    <tr>
                        <td><b>Status</b></td>
                        <td>: {statusText}</td>
                    </tr>


                    <tr>
                        <td><b>Container ETA</b></td>
                        <td>: {etaList}</td>
                    </tr>

                </table>
                <br/>


                <b>Shipment Details</b>
                <br/><br/>
                <table border='1'
                cellpadding='5'
                cellspacing='0'
                style='border-collapse:collapse;width:100%'>

                    <thead>

                        <tr style='background-color:#f2f2f2'>
                            <th style='padding:5px;text-align:left'>PO No</th>
                            <th style='padding:5px;text-align:left'>HBL No</th>
                            <th style='padding:5px;text-align:left'>Container No</th>
                            <th style='padding:5px;text-align:left'>Vendor</th>
                            <th style='padding:5px;text-align:left'>Description</th>
                            <th style='padding:5px;text-align:left'>Country</th>
                            <th style='padding:5px;text-align:left'>DEDON ETA</th>
                            <th style='padding:5px;text-align:right'>Tagged Qty</th>
                        </tr>

                    </thead>


                <tbody>

                    {rows}

                </tbody>


                </table>


                <br/>


                <p>
                    Please review and take necessary action.
                </p>


                 <p>
                    Should you encounter any issues or require further assistance,
                    please do not hesitate to contact the IT Team.
                </p>

                <hr />


                <small>
                    System generated email. Please do not reply.
                </small>


                </div>

            ";
        }


        private async Task SaveEmailHistoryAsync(
        IGrouping<string, vw_ProcurementETAEmailNotification> shipmentGroup)
        {
            var items = shipmentGroup
                .GroupBy(x => new
                {
                    x.PK_ptProcID,
                    x._PK_ptEmailNotification_ID
                })
                .Select(g => g.First())
                .ToList();


            foreach (var item in items)
            {
                _context.ShipmentEmailHistory.Add(
                    new ShipmentEmailHistory
                    {
                        _ptProcID = item.PK_ptProcID,

                        _ptEmailNotificationID =
                            item._PK_ptEmailNotification_ID,

                        _ptReminderType =
                            shipmentGroup.Key,

                        _ptLastSentDate =
                            DateTime.Now,

                        _ptCreatedDate =
                            DateTime.Now
                    });
            }


            await _context.SaveChangesAsync();
        }



        private string GetShipmentMessage(string status)
        {
            return status switch
            {
                "Upcoming" =>
                    "This is to inform you that the following shipment is scheduled to arrive within the next 14 days. Please review the shipment details and prepare accordingly.",

                "Shipment Today" =>
                    "This is to inform you that the following shipment is expected to arrive today. Please ensure the necessary receiving arrangements are completed.",

                "Due" =>
                    "This is to inform you that the following shipment has passed its ETA by less than 7 days. Please review the shipment status and follow up accordingly.",

                "Delayed" =>
                    "This is to inform you that the following shipment has been delayed beyond 7 days from the expected arrival date. Please take the necessary follow-up actions.",

                _ =>
                    "This is to inform you of the latest shipment status update. Please review the details below."
            };
        }
    }
}
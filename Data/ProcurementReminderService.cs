using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Constants;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using System.Net.Mail;

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
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Distinct()
                    .ToList();

                if (!recipients.Any())
                    return;


                var subject = GetSubject(shipmentGroup.Key);

                var body = await BuildEmailBodyAsync(shipmentGroup);


                // Generate Excel attachment
                var excelBytes = GenerateShipmentExcel(shipmentGroup);

                var fileName = $"{GetShipmentStatusText(shipmentGroup.Key)}_{DateTime.Now:yyyy-MM-dd}.xlsx";

                var attachment = new Attachment(
                    new MemoryStream(excelBytes),
                    fileName,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                );


                await _emailService.SendEmailAsync(
                    recipients,
                    subject,
                    body,
                    EmailModules.ShipmentNotification,
                    attachment
                );


                await SaveEmailHistoryAsync(shipmentGroup);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Shipment notification failed: {ex.Message}");
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
        private string GetShipmentStatusText(string status)
        {
            return status switch
            {
                "Today" => "Shipment_Arriving_Today",
                "Upcoming" => "Upcoming _Shipment_Arrival",
                "Overdue" => "Shipment_Overdue",
                "Delayed" => "Shipment_Delayed",
                _ => status
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


                var etaList = string.Join(", ",
                    shipmentGroup
                        .Select(x => x._ptContainerETA)
                        .Distinct()
                        .Select(x => x?.ToString("yyyy-MM-dd"))
                );
            

            return $@"
                <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

                    <p>Dear Team,</p>

                    <p>
                        {GetShipmentMessage(shipmentGroup.Key)}
                    </p>                                    

                    <p>
                       If you have any questions or require assistance, please contact the IT Team.
                    </p>

                    <p>
                        Thank you.
                    </p>
                    <hr/>

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
                    """
                    This is an automated notification informing you that one or more shipments are expected to arrive within the next 14 days.
                    <br/><br/>
                    Please refer to the attached Excel file for the shipment details and review the information accordingly. Kindly ensure that all necessary preparations and coordination are completed prior to the scheduled arrival.
                    """,

                "Shipment Today" =>
                    """
                    This is to inform you that the following shipment is expected to arrive today.
                    <br/><br/>
                    Please refer to the attached Excel file for the shipment details and review the information accordingly. Kindly ensure that all necessary receiving arrangements are completed and coordinate with the concerned teams to facilitate a smooth receiving process.
                    """,

                "Due" =>
                    """
                    This is to inform you that the following shipment has passed its ETA by less than seven (7) days.
                    <br/><br/>
                    Please refer to the attached Excel file for the shipment details and review the information accordingly. Kindly follow up with the concerned parties to confirm the latest shipment status and take the necessary actions to avoid further delays.
                    """,

                "Delayed" =>
                    """
                    This is an automated notification to inform you that the following shipment has been delayed by more than seven (7) days beyond its expected arrival date.
                    <br/><br/>
                    Please refer to the attached Excel file for the shipment details and review the information accordingly. Kindly coordinate with the concerned parties and take the necessary follow-up actions to determine the updated shipment status and minimize any potential impact on operations.
                    """,

                _ =>
                    "This is to inform you of the latest shipment status update. Please review the details below."
            };
        }
        private byte[] GenerateShipmentExcel(IEnumerable<vw_ProcurementETAEmailNotification> items)
        {
            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Shipment Details");

            // Headers
            worksheet.Cell(1, 1).Value = "PO No";
            worksheet.Cell(1, 2).Value = "HBL No";
            worksheet.Cell(1, 3).Value = "Container No";
            worksheet.Cell(1, 4).Value = "Vendor";
            worksheet.Cell(1, 5).Value = "Item Code";
            worksheet.Cell(1, 6).Value = "Item Description";
            worksheet.Cell(1, 7).Value = "Country";
            worksheet.Cell(1, 8).Value = "DEDON ETA";
            worksheet.Cell(1, 9).Value = "Tagged Qty";

            var header = worksheet.Range("A1:I1");
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;

            foreach (var x in items)
            {
                worksheet.Cell(row, 1).Value = x._ptDocNo;
                worksheet.Cell(row, 2).Value = x._ptHBLNo;
                worksheet.Cell(row, 3).Value = x._ptContainerNo;
                worksheet.Cell(row, 4).Value = x._ptVendorName ?? "-";
                worksheet.Cell(row, 5).Value = x._ptItemCode ?? "-";
                worksheet.Cell(row, 6).Value = x._ptItemDescription ?? "-";
                worksheet.Cell(row, 7).Value = x._ptCountryOfOrigin ?? "-";
                worksheet.Cell(row, 8).Value = x._ptContainerETA?.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 9).Value = x._ptPartialDelivered;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}
using ClosedXML.Excel;
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


            foreach (var group in notifications.GroupBy(x => x.ShipmentStatus))
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
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();


                if (!recipients.Any())
                    return;


                var status = shipmentGroup.Key;

                var subject = GetSubject(status);

                var body = BuildEmailBody(shipmentGroup);


                var items = shipmentGroup
                    .DistinctBy(x => x.PK_ptProcID)
                    .ToList();


                var excelBytes = GenerateShipmentExcel(items);


                var fileName =
                    $"{GetShipmentStatusText(status)}_{DateTime.Now:yyyy-MM-dd}.xlsx";


                using var attachment = new Attachment(
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


                await SaveReminderTrackingAsync(shipmentGroup);

                await SaveEmailHistoryAsync(shipmentGroup);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Shipment notification failed: {ex.Message}");
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

                "Overdue" =>
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
                "Shipment Today" => "Shipment_Arriving_Today",
                "Upcoming" => "Upcoming_Shipment_Arrival",
                "Overdue" => "Shipment_Overdue",
                "Delayed" => "Shipment_Delayed",
                _ => status
            };
        }

        private string BuildEmailBody(
            IGrouping<string, vw_ProcurementETAEmailNotification> shipmentGroup)
        {
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

        private async Task SaveReminderTrackingAsync(
            IGrouping<string, vw_ProcurementETAEmailNotification> shipmentGroup)
        {
            var items = shipmentGroup
                .GroupBy(x => new
                {
                    x.PK_ptProcID,
                    x._PK_ptEmailNotification_ID
                })
                .Select(x => x.First())
                .ToList();


            foreach (var item in items)
            {
                var tracking =
                    await _context.ShipmentReminderTracking
                    .FirstOrDefaultAsync(x =>
                        x._ptProcID == item.PK_ptProcID &&
                        x._ptEmailNotificationID ==
                            item._PK_ptEmailNotification_ID &&
                        x._ptReminderType ==
                            shipmentGroup.Key);


                if (tracking == null)
                {
                    _context.ShipmentReminderTracking.Add(
                        new ShipmentReminderTracking
                        {
                            _ptProcID = item.PK_ptProcID,

                            _ptEmailNotificationID =
                                item._PK_ptEmailNotification_ID,

                            _ptReminderType =
                                shipmentGroup.Key,

                            _ptFirstTriggerDate =
                                DateTime.Today,

                            _ptLastSentDate =
                                DateTime.Today,

                            _ptSendCount = 1,

                            _ptStatus = "Active",

                            _ptCreatedDate = DateTime.Now
                        });
                }
                else
                {
                    tracking._ptLastSentDate =
                        DateTime.Today;

                    tracking._ptSendCount++;

                    tracking._ptUpdatedDate =
                        DateTime.Now;
                }
            }


            await _context.SaveChangesAsync();
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
               .Select(x => x.First())
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

            var headers = new[]
            {
                "PO No",
                "HBL No",
                "Container No",
                "Vendor",
                "Item Code",
                "Item Description",
                "Country",
                "DEDON ETA",
                "Tagged Qty"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var header = worksheet.Range("A1:I1");
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;


            var row = 2;

            foreach (var x in items)
            {
                worksheet.Cell(row, 1).Value = x._ptDocNo;
                worksheet.Cell(row, 2).Value = x._ptHBLNo;
                worksheet.Cell(row, 3).Value = x._ptContainerNo;
                worksheet.Cell(row, 4).Value = x._ptVendorName ?? "-";
                worksheet.Cell(row, 5).Value = x._ptItemCode ?? "-";
                worksheet.Cell(row, 6).Value = x._ptItemDescription ?? "-";
                worksheet.Cell(row, 7).Value = x._ptCountryOfOrigin ?? "-";
                worksheet.Cell(row, 8).Value = x._ptContainerETA?.ToString("yyyy-MM-dd") ?? "-";
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
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Constants;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using System.Net.Mail;

namespace PlanningTool.NavExtension.Services;

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
        var shipments = await GetShipmentsAsync();

        if (!shipments.Any())
            return;


        var upcoming = GetUpcomingShipments(shipments);

        if (upcoming.Any())
        {
            await SendShipmentEmailAsync(
                upcoming,
                "Upcoming");
        }

        var delayed = GetDelayedShipments(shipments);

        if (delayed.Any())
        {
            await SendShipmentEmailAsync(
                delayed,
                "Delayed");
        }
    }



    private async Task<List<ProcurementCenterBalance>>GetShipmentsAsync()
    {
        return await _context.ProcurementCenterBalanceView
            .AsNoTracking()
            .Where(x => x._ptStatus == 1)
            .ToListAsync();
    }



    private List<ProcurementCenterBalance>GetUpcomingShipments(
        List<ProcurementCenterBalance> shipments)
    {
        var today = DateTime.Today;


        bool triggerReached = shipments.Any(x =>
            x._ptContainerETA.HasValue &&
            x._ptContainerETA.Value.Date == today.AddDays(14)
        );


        bool isFriday =
            today.DayOfWeek == DayOfWeek.Friday;


        if (!triggerReached && !isFriday)
            return new List<ProcurementCenterBalance>();


        return shipments
            .Where(x =>
                x._ptContainerETA.HasValue &&
                x._ptContainerETA.Value.Date >= today &&
                x._ptContainerETA.Value.Date <= today.AddDays(14)
            )
            .DistinctBy(x => x.PK_ptProcID)
            .ToList();
    }


    private List<ProcurementCenterBalance> GetDelayedShipments(
    List<ProcurementCenterBalance> shipments)
    {
        var today = DateTime.Today;

        return shipments
            .Where(x =>
                x._ptContainerETA.HasValue &&
                x._ptContainerETA.Value.Date.AddDays(7) <= today)
            .DistinctBy(x => x.PK_ptProcID)
            .ToList();
    }


    private async Task SendShipmentEmailAsync(
        List<ProcurementCenterBalance> shipments,
        string reminderType)
    {
        var recipientUsers = await GetRecipientsAsync(reminderType);


        if (!recipientUsers.Any())
            return;


        // Convert for EmailService
        var recipients = recipientUsers
            .Select(x => x.Email)
            .Distinct()
            .ToList();


        var subject =
            GetSubject(reminderType);


        var body =
            BuildEmailBody(reminderType);


        var excel =
            GenerateShipmentExcel(shipments);


        var etaDates = shipments
             .Where(s => s._ptContainerETA.HasValue)
             .Select(s => s._ptContainerETA!.Value)
             .ToList();

        var fileName = etaDates.Any()
            ? $"{reminderType}_Shipment_ETA_{etaDates.Min():yyyy-MM-dd}_to_{etaDates.Max():yyyy-MM-dd}.xlsx"
            : $"{reminderType}_Shipment_{DateTime.Now:yyyy-MM-dd}.xlsx";

        using var attachment = new Attachment(
            new MemoryStream(excel),
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


        // Save with EmailNotification ID for FK
        await SaveEmailHistoryAsync(
            shipments,
            recipientUsers,
            reminderType);
    }



    private async Task<List<EmailRecipient>> GetRecipientsAsync(
    string reminderType)
    {
        return await _context.EmailNotifications
            .Where(x =>
                x._ptStatus == "Active" &&
                (
                    reminderType == "Upcoming"
                        ? x._pt2WeeksReminder
                        : x._ptDelaysReminder
                ))
            .Select(x => new EmailRecipient
            {
                Id = x._PK_ptEmailNotification_ID,
                Email = x._ptEmailAddress
            })
            .ToListAsync();
    }



    private string GetSubject(string type)
    {
        return type switch
        {
            "Upcoming" =>
                "DEDON ETA | Upcoming Shipment Notification",

            "Delayed" =>
                "DEDON ETA | Delayed Shipment Notification",

            _ =>
                "DEDON Procurement Notification"
        };
    }



    private string BuildEmailBody(string type)
    {
        var message = type switch
        {
            "Upcoming" =>
            """
                This is an automated notification informing you that one or more shipments are expected to arrive within the next 14 days.
                <br/><br/>
                Please refer to the attached Excel file for the shipment details and review the information accordingly. 
                Kindly ensure that all necessary preparations and coordination are completed prior to the scheduled arrival.
            """,


            "Delayed" =>
            """
                This is an automated notification to inform you that the following shipment has been delayed by more than seven (7) days 
                beyond its expected arrival date.
                <br/><br/>
                Please refer to the attached Excel file for the shipment details and review the information accordingly. 
                Kindly coordinate with the concerned parties and take the necessary follow-up actions to 
                determine the updated shipment status and minimize any potential impact on operations.
            """,


            _ => ""
        };


        return $"""
        <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

            <p>Dear Team,</p>

            <p>
                {message}
            </p>

            <p>
                If you have any questions, please contact the IT Team.
            </p>

            <p>
                Thank you.
            </p>

            <hr/>

            <small>
                System generated email. Please do not reply.
            </small>

        </div>
        """;
    }

    public class EmailRecipient
    {
        public int Id { get; set; }

        public string Email { get; set; } = "";
    }


    private async Task SaveEmailHistoryAsync(
    List<ProcurementCenterBalance> shipments,
    List<EmailRecipient> recipients,
    string type)
    {
        foreach (var shipment in shipments)
        {
            foreach (var recipient in recipients)
            {
                _context.ShipmentEmailHistory.Add(
                    new ShipmentEmailHistory
                    {
                        _ptProcID = shipment.PK_ptProcID,

                        _ptEmailNotificationID = recipient.Id,

                        _ptReminderType = type,

                        _ptLastSentDate = DateTime.Now,

                        _ptCreatedDate = DateTime.Now
                    });
            }
        }

        await _context.SaveChangesAsync();
    }

    private byte[] GenerateShipmentExcel(
    IEnumerable<ProcurementCenterBalance> items)
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add("Shipment Details");


        var headers = new[]
        {
            "PO No",
            "Line No",
            "HBL No",
            "Container No",
            "Vendor",
            "Item Code",
            "Item Description",
            "PO Outstanding Qty",
            "Tagged Qty",
            "Balance Qty",
            "Country",
            "ETA",
            "Remarks"
        };


        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }


        var headerRange = sheet.Range("A1:M1");

        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;


        int row = 2;


        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item._ptDocNo;
            sheet.Cell(row, 2).Value = item._ptLineNo;
            sheet.Cell(row, 3).Value = item._ptHBLNo;
            sheet.Cell(row, 4).Value = item._ptContainerNo;
            sheet.Cell(row, 5).Value = item._ptVendorName;
            sheet.Cell(row, 6).Value = item._ptItemCode;
            sheet.Cell(row, 7).Value = item._ptItemDescription;
            sheet.Cell(row, 8).Value = item._ptOutstandingQty;
            sheet.Cell(row, 9).Value = item._ptPartialDelivered;
            sheet.Cell(row, 10).Value = item._ptBalanceQty;
            sheet.Cell(row, 11).Value = item._ptCountryOfOrigin;

            sheet.Cell(row, 12).Value =
                item._ptContainerETA?.ToString("yyyy-MM-dd");

            sheet.Cell(row, 13).Value = item._ptRemarks;


            row++;
        }


        sheet.Columns().AdjustToContents();


        using var stream = new MemoryStream();

        workbook.SaveAs(stream);


        return stream.ToArray();
    }
}
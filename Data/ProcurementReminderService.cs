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
            // Generate reminder records
            await _context.Database.ExecuteSqlRawAsync("EXEC _spGenerateProcurementReminders");

            // Get email settings
            var emailSettings = await _context.tblEmailNotification.FirstOrDefaultAsync();

            if (emailSettings == null)
                return;

            // Get pending reminders
            var reminders = await _context.tblReminderNotification
                .Where(x =>
                    x.EmailStatus == "Pending" &&
                    !string.IsNullOrEmpty(x.RecipientEmail) &&
                    x.ContainerETA.HasValue)
                .ToListAsync();

            foreach (var reminder in reminders)
            {
                try
                {
                    if (!reminder.ContainerETA.HasValue)
                        continue;


                    var shouldSend = reminder.ReminderType switch
                    {
                        "2WeeksReminder" => emailSettings._pt2WeeksReminder,
                        "DelaysReminder" => emailSettings._ptDelaysReminder,
                        _ => false
                    };

                    if (!shouldSend)
                        continue;

                    var reminderItems = await _context.GetTaggedItems
                        .Where(p =>
                            p._ptDocNo == reminder.PONo &&
                            p._ptHBLNo == reminder.HBLNo &&
                            p._ptContainerNo == reminder.ContainerNo &&
                            p._ptContainerETA == reminder.ContainerETA &&
                            p._ptStatus == 1)
                        .Select(p => new
                        {
                            p._ptDocNo,
                            p._ptItemCode,
                            p._ptItemDescription,
                            p._ptPartialDelivered

                        }).ToListAsync();


                    var itemRows = string.Join("",
                        reminderItems.Select(x => $@"
                        <tr>
                            <td style='padding:4px 4px;'>{x._ptDocNo}</td>
                            <td style='padding:4px 4px;'>{x._ptItemCode}</td>
                            <td style='padding:4px 4px;'>{x._ptItemDescription ?? "-"}</td>
                            <td style='padding:4px 4px;text-align:right;'>{x._ptPartialDelivered:N2}</td>
                        </tr>"));


                    var subject = reminder.ReminderType switch
                    {
                        "2WeeksReminder" => "DEDON ETA | 2 Weeks Notification",
                        "DelaysReminder" => "DEDON ETA | Delayed Notification",
                        _ => "Procurement Notification"
                    };


                    var message = reminder.ReminderType == "2WeeksReminder"
                        ? "This is a friendly reminder that the following shipment is expected to arrive within the next <strong>14 days</strong>."
                        : "This is to inform you that the ETA of the following shipment has already passed and requires immediate attention.";


                    var daysUntilETA = (reminder.ContainerETA.Value.Date - DateTime.Today).Days;

                    var status = daysUntilETA >= 0
                        ? $"Arriving Within {daysUntilETA} Days"
                        : $"Delayed by {Math.Abs(daysUntilETA)} Days";


                    var action = reminder.ReminderType == "2WeeksReminder"
                        ? "Please ensure the necessary preparations and coordination are completed prior to the shipment arrival."
                        : "Kindly review the shipment status and take the necessary actions if follow-up is required.";

                    var body = $@"
                        <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#333333;'>

                            <p>Dear Team,</p>

                            <p>
                                {message}
                            </p>


                            <table>

                                <tr>
                                    <th style='text-align:left;'>HBL No</th>
                                    <td>: {reminder.HBLNo}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Container No</th>
                                    <td>: {reminder.ContainerNo}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Country Of Origin</th>
                                    <td>: {reminder.CountryOfOrigin}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Dedon ETA</th>
                                    <td>: {reminder.ContainerETA:yyyy-MM-dd}</td>
                                </tr>

                                <tr>
                                    <th style='text-align:left;'>Status</th>
                                    <td>: {status}</td>
                                </tr>

                            </table>


                            <p><strong>Purchase Order Details:</strong></p>


                            <table border='1' cellpadding='4' cellspacing='0'
                                    style='border-collapse:collapse;width:70%;'>

                                <thead>
                                    <tr>
                                        <th style='text-align:left;'>PO No</th>
                                        <th style='text-align:left;'>Item Code</th>
                                        <th style='text-align:left;'>Item Description</th>
                                        <th style='text-align:right;'>Expected Qty</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {itemRows}
                                </tbody>

                            </table>

                            <p>
                                {action}
                            </p>
                            <p>
                                Should you encounter any issues or require further assistance,
                                please do not hesitate to contact the IT Team.
                            </p>

                            <hr />

                            <small>
                                This is a system-generated email. Please do not reply.
                            </small>

                        </div>"
                    ;

                    var recipients = reminder.RecipientEmail
                        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();


                    await _emailService.SendEmailAsync(
                        recipients,
                        subject,
                        body,
                        EmailModules.ShipmentNotification
                    );


                    reminder.EmailStatus = "Sent";
                    reminder.SentDate = DateTime.Now;
                }
                catch
                {
                    reminder.EmailStatus = "Failed";
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
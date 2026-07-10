using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;

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
                    var days = (reminder.ContainerETA.Value.Date - DateTime.Today).Days;

                    var shouldSend = reminder.ReminderType switch
                    {
                        "2WeeksReminder" => emailSettings._pt2WeeksReminder == true &&
                                            days >= 0 &&
                                            days <= 14,

                        "DelaysReminder" => emailSettings._ptDelaysReminder == true &&
                                            days < 0,

                        _ => false
                    };

                    if (!shouldSend)
                        continue;

                    var subject = reminder.ReminderType switch
                    {
                        "2WeeksReminder" => "Dedon ETA Reminder",
                        "DelaysReminder" => "Delayed Dedon ETA Notification",
                        _ => "Procurement Notification"
                    };

                    var body = reminder.ReminderType == "2WeeksReminder"
                    ? $@"
                    <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff;'>

                        <p>Dear Team,</p>

                        <p>
                            This is a friendly reminder that the following shipment is expected to arrive
                            within the next <strong>14 days</strong>.
                        </p>

                        <table>

                            <tr>
                                <th style='text-align:left;'>HBL No</th>
                                <td style='text-align:left;'>: {reminder.HBLNo}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>Container No</th>
                                <td style='text-align:left;'>: {reminder.ContainerNo}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>PO No</th>
                                <td style='text-align:left;'>: {reminder.PONo}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>Dedon ETA</th>
                                <td style='text-align:left;'>: {reminder.ContainerETA:yyyy-MM-dd}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>Status</th>
                                <td style='text-align:left;'>: Arriving Within 2 Weeks</td>
                            </tr>

                        </table>

                        <p>
                            Please ensure the necessary preparations and coordination are completed
                            prior to the shipment arrival.
                        </p>
                        <hr />

                        <small>
                            This is a system-generated email. Please do not reply.
                        </small>

                    </div>"
                    : $@"
                    <div style='font-family:'Segoe UI', Arial, sans-serif; font-size:12px; font-weight:400; line-height:1.5; color:#ffffff;'>

                        <p>Dear Team,</p>

                        <p>
                            This is to inform you that the ETA of the following shipment has already
                            passed and may require immediate attention.
                        </p>

                        <table>

                            <tr>
                                <th style='text-align:left;'>HBL No</th>
                                <td style='text-align:left;'>: {reminder.HBLNo}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>Container No</th>
                                <td style='text-align:left;'>: {reminder.ContainerNo}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>PO No</th>
                                <td style='text-align:left;'>: {reminder.PONo}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>Dedon ETA</th>
                                <td style='text-align:left;'>: {reminder.ContainerETA:yyyy-MM-dd}</td>
                            </tr>

                            <tr>
                                <th style='text-align:left;'>Status</th>
                                <td style='text-align:left;'>: Delayed</td>
                            </tr>

                        </table>

                        <p>
                            Kindly review the shipment status and take the necessary actions
                            if follow-up is required.
                        </p>
                        <hr />

                        <small>
                            This is a system-generated email. Please do not reply.
                        </small>

                    </div>";

                    var recipients = reminder.RecipientEmail
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                    await _emailService.SendEmailAsync(
                        recipients,
                        subject,
                        body);

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
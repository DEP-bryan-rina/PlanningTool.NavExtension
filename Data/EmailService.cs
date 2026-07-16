using System.Net;
using System.Net.Mail;

namespace PlanningTool.NavExtension.Data
{
    public class EmailService
    {
        // ✅ single recipient

        public async Task SendEmailAsync(
        List<string> recipients,
        string subject,
        string body,
        string fromName)
        {
            using var smtpClient = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    "no-reply@dedon.ph",
                    "Ayk,Dhbfi1990bBD")
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress("no-reply@dedon.ph", fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            recipients.ForEach(mailMessage.To.Add);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}

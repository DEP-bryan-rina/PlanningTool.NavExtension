using Microsoft.Extensions.Options;
using PlanningTool.NavExtension.Data;
using System.Net;
using System.Net.Mail;

public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(
        List<string> recipients,
        string subject,
        string body,
        string fromName,
        Attachment? attachment = null,
        bool isHighImportance = false)
    {
        using var smtpClient = new SmtpClient(_settings.Host)
        {
            Port = _settings.Port,
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _settings.Username,
                _settings.Password)
        };

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.Username, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            Priority = isHighImportance
                ? MailPriority.High
                : MailPriority.Normal
        };

        foreach (var recipient in recipients.Distinct())
        {
            mailMessage.To.Add(recipient);
        }

        if (attachment != null)
        {
            mailMessage.Attachments.Add(attachment);
        }

        await smtpClient.SendMailAsync(mailMessage);
    }
}
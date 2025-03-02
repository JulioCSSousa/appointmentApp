using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailSender : IEmailSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;

    public EmailSender(string smtpHost, int smtpPort, string smtpUser, string smtpPass)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _smtpUser = smtpUser;
        _smtpPass = smtpPass;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        using (var client = new SmtpClient(_smtpHost, _smtpPort))
        {
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
            client.EnableSsl = true; // Necessário para conexões seguras
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpUser),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);
            await client.SendMailAsync(mailMessage);
        }
    }
}

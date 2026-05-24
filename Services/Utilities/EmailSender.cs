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
            // 1. PRIMEIRO desative as credenciais padrão
            client.UseDefaultCredentials = false;

            // 2. DEPOIS insira a sua App Password e o seu e-mail do Gmail
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);

            client.EnableSsl = true;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            // 3. DINÂMICO: Ajusta o TargetName de acordo com o Host que vier do appsettings (Gmail ou Mailtrap)
            client.TargetName = $"STARTTLS/{_smtpHost}";

            var mailMessage = new MailMessage
            {
                // O Gmail exige que o 'From' seja exatamente o e-mail autenticado no _smtpUser
                From = new MailAddress(_smtpUser, "Agendamento Salão"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
    }
}


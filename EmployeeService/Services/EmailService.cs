using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
{
    try
    {
        var smtpHost = _configuration["EmailSettings:SmtpServer"];
        var smtpPort = _configuration["EmailSettings:SmtpPort"];
        var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
        var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
        var fromEmail = _configuration["EmailSettings:FromEmail"];

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPort) ||
            string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
        {
            throw new InvalidOperationException("Missing or invalid SMTP settings in appsettings.json");
        }

        int port = int.Parse(smtpPort);
        using (var smtpClient = new SmtpClient(smtpHost, port))
        {
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            smtpClient.EnableSsl = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;

            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(fromEmail);
                mailMessage.To.Add(new MailAddress(toEmail));
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = false;

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"✅ Email successfully sent to {toEmail}");
            }
        }
    }
    catch (SmtpException smtpEx)
    {
        _logger.LogError($" SMTP Error sending email to {toEmail}: {smtpEx.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError($" General Error sending email to {toEmail}: {ex.Message}");
    }
}

}

using System.Net;
using System.Net.Mail;

namespace WeatherApp.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        
        var fromEmail = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];

        var smtpClient = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            Credentials = new NetworkCredential(fromEmail, password),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };
        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }
}
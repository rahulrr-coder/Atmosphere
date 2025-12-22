using System.Net.Mail;

namespace WeatherApp.Services.Wrappers;

public interface ISmtpClientWrapper
{
    Task SendMailAsync(MailMessage message);
}
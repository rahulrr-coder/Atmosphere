using System.Net;
using System.Net.Mail;

namespace WeatherApp.Services.Wrappers;

public class SmtpClientWrapper : ISmtpClientWrapper
{
    private readonly string _host = "smtp.gmail.com";
    private readonly int _port = 587;
    private readonly string _username;
    private readonly string _password;

    public SmtpClientWrapper(IConfiguration config)
    {
        _username = config["Email:Username"] ?? "";
        _password = config["Email:Password"] ?? "";
    }

    public async Task SendMailAsync(MailMessage message)
    {
        using var client = new SmtpClient(_host)
        {
            Port = _port,
            Credentials = new NetworkCredential(_username, _password),
            EnableSsl = true,
        };

        await client.SendMailAsync(message);
    }
}
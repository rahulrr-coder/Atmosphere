using System.Net;
using System.Net.Mail;
using WeatherApp.Models;

namespace WeatherApp.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendWeatherEmailAsync(string toEmail, WeatherModel weather, string aiAdvice);
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

        if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("❌ Email credentials missing in appsettings.json");
            return;
        }

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

        try 
        {
            await smtpClient.SendMailAsync(mailMessage);
            Console.WriteLine($"✅ Email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to send email: {ex.Message}");
        }
    }

    public async Task SendWeatherEmailAsync(string toEmail, WeatherModel weather, string aiAdvice)
    {
        var subject = $"Your Daily Forecast for {weather.City} 🌤️";
        
        // Clean JSON formatting from advice if present
        var cleanAdvice = aiAdvice.Replace("```json", "").Replace("```", "").Trim();
        // Note: For email simplicity, we just dump the text. 
        // Ideally, we'd parse the JSON here too, but raw text is fine for the MVP step.

        var body = $@"
        <div style='font-family: sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; border-radius: 12px; overflow: hidden; color: #333;'>
            <div style='background-color: #4CAF50; color: white; padding: 25px; text-align: center;'>
                <h1>Good Morning! ☀️</h1>
                <p>Here is your update for {weather.City}</p>
            </div>
            <div style='padding: 25px;'>
                <div style='display: flex; justify-content: space-between; margin-bottom: 25px;'>
                    <div style='text-align: center; width: 48%; background: #f9f9f9; padding: 15px; border-radius: 8px;'>
                        <span style='font-size: 28px; display: block; font-weight: bold;'>{weather.CurrentTemp:F0}°C</span>
                        <span style='color: #666;'>{weather.CurrentCondition}</span>
                    </div>
                    <div style='text-align: center; width: 48%; background: #f9f9f9; padding: 15px; border-radius: 8px;'>
                        <span style='font-size: 28px; display: block; font-weight: bold; color: #e67e22;'>{weather.AQI}</span>
                        <span style='color: #666;'>AQI</span>
                    </div>
                </div>

                <h3 style='color: #2c3e50;'>💡 Daily Insight</h3>
                <div style='background-color: #e8f5e9; border-left: 5px solid #4CAF50; padding: 15px; font-style: italic; color: #555;'>
                    {cleanAdvice}
                </div>

                <p style='text-align: center; margin-top: 30px; font-size: 16px; color: #777;'>
                    Have a wonderful day! 🌟
                </p>
            </div>
        </div>";

        await SendEmailAsync(toEmail, subject, body);
    }
}
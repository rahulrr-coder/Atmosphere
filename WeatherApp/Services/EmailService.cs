using System.Net;
using System.Net.Mail;
using System.Text.Json;
using WeatherApp.Models;
using WeatherApp.Services.Wrappers; // Make sure you created the wrapper in this namespace

namespace WeatherApp.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendWeatherEmailAsync(string toEmail, WeatherModel weather, string aiAdvice);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ISmtpClientWrapper _smtpClient; // 👈 Dependency Injection

    // We inject the wrapper here instead of creating it inside
    public EmailService(IConfiguration configuration, ISmtpClientWrapper smtpClient)
    {
        _configuration = configuration;
        _smtpClient = smtpClient;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var fromEmail = _configuration["Email:Username"];
        
        // Note: Password is handled inside the SmtpClientWrapper, so we don't need it here.

        if (string.IsNullOrEmpty(fromEmail))
        {
            Console.WriteLine("❌ Email credentials missing.");
            return;
        }

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, "Atmosphere Daily"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };
        mailMessage.To.Add(toEmail);

        try 
        {
            // 🟢 This calls the wrapper (Mockable in tests, Real SmtpClient in prod)
            await _smtpClient.SendMailAsync(mailMessage);
            Console.WriteLine($"✅ Email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to send email: {ex.Message}");
        }
    }

    public async Task SendWeatherEmailAsync(string toEmail, WeatherModel weather, string aiAdvice)
    {
        var subject = $"The Atmosphere in {weather.City} ☁️";
        
        // 1. Robust AI JSON Parsing
        string summary = "Enjoy the atmosphere.";
        string outfit = "Dress comfortably.";
        string safety = "No specific hazards.";

        try 
        {
            // Clean markdown if present
            var cleanJson = aiAdvice.Replace("```json", "").Replace("```", "").Trim();
            
            // Extract JSON object if wrapped in text
            int start = cleanJson.IndexOf('{');
            int end = cleanJson.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                cleanJson = cleanJson.Substring(start, end - start + 1);
            }

            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("summary", out var s)) summary = s.GetString() ?? summary;
            if (root.TryGetProperty("outfit", out var o)) outfit = o.GetString() ?? outfit;
            if (root.TryGetProperty("safety", out var safe)) safety = safe.GetString() ?? safety;
        }
        catch 
        {
            // Fallback: Use raw text if parsing fails entirely
            summary = aiAdvice; 
        }

        
        var body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                /* Client-specific resets */
                body, table, td, a {{ -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; }}
                table, td {{ mso-table-lspace: 0pt; mso-table-rspace: 0pt; }}
                img {{ -ms-interpolation-mode: bicubic; }}
                body {{ height: 100% !important; margin: 0 !important; padding: 0 !important; width: 100% !important; font-family: 'Georgia', serif; background-color: #f4f4f4; }}
            </style>
        </head>
        <body style='margin: 0; padding: 0; background-color: #f4f4f4;'>
            
            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                <tr>
                    <td align='center' style='padding: 40px 10px;'>
                        <table border='0' cellpadding='0' cellspacing='0' width='600' style='background-color: #fdfbf7; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.05);'>
                            
                            <tr>
                                <td align='center' style='background-color: #1a1a1a; padding: 40px; color: #ffffff;'>
                                    <span style='font-size: 24px; font-weight: bold; letter-spacing: -1px; display: block; margin-bottom: 10px;'>Atmosphere.</span>
                                    <h1 style='margin: 0; font-size: 36px; font-weight: normal;'>{weather.City}</h1>
                                </td>
                            </tr>

                            <tr>
                                <td align='center' style='padding: 40px 20px 20px 20px;'>
                                    <span style='font-size: 64px; font-weight: bold; color: #1a1a1a; display: block;'>{weather.CurrentTemp:F0}°</span>
                                    <span style='font-size: 18px; color: #666; font-style: italic; display: block; margin-top: 5px;'>{weather.CurrentCondition}</span>
                                </td>
                            </tr>

                            <tr>
                                <td style='padding: 0 40px 40px 40px;'>
                                    <table border='0' cellpadding='0' cellspacing='0' width='100%' style='border-top: 1px solid #eee; border-bottom: 1px solid #eee;'>
                                        <tr>
                                            <td align='center' style='padding: 20px 0; width: 33%; border-right: 1px solid #eee;'>
                                                <span style='font-family: sans-serif; font-size: 10px; color: #999; letter-spacing: 1px; text-transform: uppercase; display: block; margin-bottom: 5px;'>HUMIDITY</span>
                                                <span style='font-family: sans-serif; font-size: 16px; font-weight: bold; color: #333;'>{weather.Humidity}%</span>
                                            </td>
                                            <td align='center' style='padding: 20px 0; width: 33%; border-right: 1px solid #eee;'>
                                                <span style='font-family: sans-serif; font-size: 10px; color: #999; letter-spacing: 1px; text-transform: uppercase; display: block; margin-bottom: 5px;'>WIND</span>
                                                <span style='font-family: sans-serif; font-size: 16px; font-weight: bold; color: #333;'>{weather.WindSpeed:F1} km/h</span>
                                            </td>
                                            <td align='center' style='padding: 20px 0; width: 33%;'>
                                                <span style='font-family: sans-serif; font-size: 10px; color: #999; letter-spacing: 1px; text-transform: uppercase; display: block; margin-bottom: 5px;'>AQI</span>
                                                <span style='font-family: sans-serif; font-size: 16px; font-weight: bold; color: #333;'>{weather.AQI}</span>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>

                            <tr>
                                <td style='padding: 0 40px 40px 40px;'>
                                    
                                    <div style='margin-bottom: 30px;'>
                                        <p style='font-family: sans-serif; font-size: 12px; letter-spacing: 2px; color: #d4a373; text-transform: uppercase; font-weight: bold; margin: 0 0 15px 0; border-bottom: 2px solid #d4a373; display: inline-block; padding-bottom: 5px;'>DAILY BRIEFING</p>
                                        <p style='font-size: 18px; line-height: 1.6; color: #444; margin: 0; font-style: italic;'>""{summary}""</p>
                                    </div>

                                    <div style='margin-bottom: 30px;'>
                                        <p style='font-family: sans-serif; font-size: 12px; letter-spacing: 2px; color: #d4a373; text-transform: uppercase; font-weight: bold; margin: 0 0 15px 0; border-bottom: 2px solid #d4a373; display: inline-block; padding-bottom: 5px;'>OUTFIT</p>
                                        <p style='font-size: 16px; line-height: 1.6; color: #444; margin: 0;'>{outfit}</p>
                                    </div>

                                    <div>
                                        <p style='font-family: sans-serif; font-size: 12px; letter-spacing: 2px; color: #d4a373; text-transform: uppercase; font-weight: bold; margin: 0 0 15px 0; border-bottom: 2px solid #d4a373; display: inline-block; padding-bottom: 5px;'>ADVISORY</p>
                                        <p style='font-size: 16px; line-height: 1.6; color: #444; margin: 0;'>{safety}</p>
                                    </div>

                                </td>
                            </tr>

                            <tr>
                                <td align='center' style='background-color: #f9f9f9; padding: 30px; font-family: sans-serif; font-size: 12px; color: #aaa;'>
                                    <p style='margin: 0 0 10px 0;'>Have a wonderful day.</p>
                                    <a href='http://localhost:5173' style='color: #d4a373; text-decoration: none;'>View Dashboard</a>
                                </td>
                            </tr>

                        </table>
                    </td>
                </tr>
            </table>

        </body>
        </html>";

        await SendEmailAsync(toEmail, subject, body);
    }
}
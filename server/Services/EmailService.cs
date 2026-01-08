using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using WeatherApp.Models;
using WeatherApp.Services.Wrappers;

namespace WeatherApp.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendWeatherEmailAsync(string toEmail, WeatherModel weather, string aiAdvice);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ISmtpClientWrapper _smtpClient;
    // Define the path to the template
    private readonly string _templatePath;
    private readonly string _dashboardUrl;

    public EmailService(IConfiguration configuration, ISmtpClientWrapper smtpClient, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _smtpClient = smtpClient;
        // Use IWebHostEnvironment to get the robust path across Windows/Linux/Docker
        _templatePath = Path.Combine(env.ContentRootPath, "Templates", "WeatherEmail.html");
        _dashboardUrl = configuration["App:DashboardUrl"] ?? "http://localhost:5173";
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var fromEmail = _configuration["Email:Username"];
        if (string.IsNullOrEmpty(fromEmail))
        {
            Console.WriteLine("❌ Email credentials missing.");
            return;
        }

        // Validate email format
        if (!IsValidEmail(toEmail))
        {
            Console.WriteLine($"❌ Invalid email format: {toEmail}");
            return;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, "Atmosphere Daily"),
            Subject = SanitizeSubject(subject),
            Body = body,
            IsBodyHtml = true,
        };
        mailMessage.To.Add(toEmail);

        try
        {
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
        
        // 1. Parse the AI Response
        var (summary, outfit, safety) = ParseAiJson(aiAdvice);

        // 2. Load the Template
        string body;
        if (File.Exists(_templatePath))
        {
            body = await File.ReadAllTextAsync(_templatePath);
        }
        else
        {
            // Fallback if file is missing (Safety net)
            Console.WriteLine("⚠️ Template file not found, using fallback.");
            body = "<h1>{{City}}</h1><p>{{Summary}}</p>"; 
        }

        // 3. Replace Placeholders with HTML-encoded values to prevent XSS
        body = body
            .Replace("{{City}}", HtmlEncode(weather.City))
            .Replace("{{Temp}}", HtmlEncode(weather.CurrentTemp.ToString("F0")))
            .Replace("{{Condition}}", HtmlEncode(weather.CurrentCondition))
            .Replace("{{Humidity}}", HtmlEncode(weather.Humidity.ToString()))
            .Replace("{{Wind}}", HtmlEncode(weather.WindSpeed.ToString("F1")))
            .Replace("{{AQI}}", HtmlEncode(weather.AQI.ToString()))
            .Replace("{{Summary}}", HtmlEncode(summary))
            .Replace("{{Outfit}}", HtmlEncode(outfit))
            .Replace("{{Safety}}", HtmlEncode(safety))
            .Replace("{{DashboardLink}}", _dashboardUrl);

        await SendEmailAsync(toEmail, subject, body);
    }

    // Helper method to keep the main logic clean
    private (string Summary, string Outfit, string Safety) ParseAiJson(string aiAdvice)
    {
        string summary = "Enjoy the atmosphere.";
        string outfit = "Dress comfortably.";
        string safety = "No specific hazards.";

        try
        {
            var cleanJson = aiAdvice.Replace("```json", "").Replace("```", "").Trim();
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
            // If parsing fails, sanitize before assigning to prevent XSS
            summary = aiAdvice.Length > 200 ? aiAdvice.Substring(0, 200) + "..." : aiAdvice;
        }

        return (summary, outfit, safety);
    }

    // Security helper methods
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizeSubject(string subject)
    {
        // Remove newlines to prevent header injection
        return subject.Replace("\r", "").Replace("\n", "");
    }

    private static string HtmlEncode(string text)
    {
        return HtmlEncoder.Default.Encode(text ?? "");
    }
}   
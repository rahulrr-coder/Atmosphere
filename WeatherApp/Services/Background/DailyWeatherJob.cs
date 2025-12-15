using Quartz;
using WeatherApp.Services;
// using WeatherApp.Models; // You might not strictly need this if namespace is shared, but keep it if unsure.

namespace WeatherApp.Services.Background;

public class DailyWeatherJob : IJob
{
    private readonly IEmailService _emailService;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<DailyWeatherJob> _logger;

    public DailyWeatherJob(IEmailService emailService, IWeatherService weatherService, ILogger<DailyWeatherJob> logger)
    {
        _emailService = emailService;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting Daily Weather Job...");

        var city = "Coimbatore";
        // This now calls the fixed method name
        var weather = await _weatherService.GetWeatherAsync(city);

        if (weather != null)
        {
            string subject = $"☀️ Morning Briefing: {city}";
            
            // 👇 FIX: Use YOUR clean model properties (Temperature, Weather, etc.)
            // No more weather.Main.Temp!
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #0056b3;'>Good Morning! ☕</h2>
                    <p>Here is the current weather in <strong>{city}</strong>:</p>
                    <table style='width: 100%;'>
                        <tr>
                            <td><strong>Temperature:</strong></td>
                            <td>{weather.Temperature}°C</td>
                        </tr>
                        <tr>
                            <td><strong>Condition:</strong></td>
                            <td>{weather.Weather}</td>
                        </tr>
                        <tr>
                            <td><strong>Humidity/Precip:</strong></td>
                            <td>{weather.Precipitation}%</td>
                        </tr>
                    </table>
                    <p><em>Sent by your .NET Bot 🤖</em></p>
                </div>
            ";

            await _emailService.SendEmailAsync("user@gmail.com", subject, body);
            _logger.LogInformation($"✅ Weather Report sent for {city}!");
        }
        else
        {
            _logger.LogError("❌ Failed to fetch weather data.");
        }
    }
}
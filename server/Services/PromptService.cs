using WeatherApp.Models;

namespace WeatherApp.Services;

public interface IPromptService
{
    Task<string> BuildWeatherPromptAsync(WeatherModel weather);
}

public class PromptService : IPromptService
{
    private readonly string _templatePath;
    private string? _cachedTemplate;

    public PromptService(IWebHostEnvironment env)
    {
        _templatePath = Path.Combine(env.ContentRootPath, "Templates", "AIPrompt.txt");
    }

    public async Task<string> BuildWeatherPromptAsync(WeatherModel weather)
    {
        // Load template once and cache it
        if (_cachedTemplate == null)
        {
            if (File.Exists(_templatePath))
            {
                _cachedTemplate = await File.ReadAllTextAsync(_templatePath);
            }
            else
            {
                // Fallback if template is missing
                _cachedTemplate = "You are a weather advisor. Provide advice for {{City}} with {{Temp}}°C.";
            }
        }

        // Replace placeholders with actual weather data
        return _cachedTemplate
            .Replace("{{City}}", weather.City)
            .Replace("{{Country}}", weather.Country)
            .Replace("{{Temp}}", weather.CurrentTemp.ToString("F0"))
            .Replace("{{Condition}}", weather.CurrentCondition)
            .Replace("{{Humidity}}", weather.Humidity.ToString())
            .Replace("{{Wind}}", weather.WindSpeed.ToString("F1"))
            .Replace("{{AQI}}", weather.AQI.ToString());
    }
}

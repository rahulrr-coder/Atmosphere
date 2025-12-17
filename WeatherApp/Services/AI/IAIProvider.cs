using WeatherApp.Models;

namespace WeatherApp.Services.AI;

public interface IAIProvider
{
    string Name { get; }
    // Returns null if it fails, so the system knows to try the next one
    Task<string?> GetWeatherInsightAsync(WeatherModel weather, string prompt);
}
using WeatherApp.Models;
namespace WeatherApp.Services;

public interface IWeatherService
{
    Task<WeatherModel?> GetWeatherAsync(string city);
}


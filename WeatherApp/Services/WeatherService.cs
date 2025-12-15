using System.Net.Http.Json;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    // RENAMED from GetWeatherForCity to GetWeatherAsync to match Interface
    public async Task<WeatherModel?> GetWeatherAsync(string cityName)
    {
        var apiKey = _configuration["OpenWeather:ApiKey"];
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&units=metric&appid={apiKey}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return null; // Return null so the Job knows it failed
        }

        var externalData = await response.Content.ReadFromJsonAsync<OpenWeatherData>();

        // TRANSLATION: External Data -> Your Clean WeatherModel
        return new WeatherModel
        {
            City = externalData.name,
            // Note: Your model uses 'int', so we cast the double to int
            Temperature = (int)externalData.main.temp, 
            Weather = externalData.weather[0].main,
            Precipitation = externalData.main.humidity,
            Aqi = 0 // Placeholder until you implement AQI API
        };
    }

    // --- Helper Classes for OpenWeatherMap JSON ---
    public class OpenWeatherData
    {
        public string name { get; set; }
        public MainData main { get; set; }
        public WeatherDescription[] weather { get; set; }
    }

    public class MainData
    {
        public double temp { get; set; }
        public int humidity { get; set; }
    }

    public class WeatherDescription
    {
        public string main { get; set; }
        public string description { get; set; }
    }
}
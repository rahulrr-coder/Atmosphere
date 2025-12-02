using System.Net.Http.Json; // 1. Tool to read JSON
using WeatherApp.Models;    // 2. Link to our "Clean Box"

namespace WeatherApp.Services;

public class WeatherService : IWeatherService
{
    // The Tools we need
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    // 3. Dependency Injection: "Hey App, give me a web browser (HttpClient) and the settings (Configuration)"
    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }
    
    // 4. The Main Method: Fetch data and translate it
    public async Task<WeatherModel> GetWeatherForCity(string cityName)
    {
        // Step A: Get the secure API Key from appsettings.json
        var apiKey = _configuration["OpenWeather:ApiKey"];
        // Step B: Build the URL (Where are we going?)
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&units=metric&appid={apiKey}";

        // Step C: Call the internet! (We wait/await because it takes time)
        var response = await _httpClient.GetAsync(url);

        // Step D: Check if it failed (Typo in city name? Internet down?)
        if (!response.IsSuccessStatusCode)
        {
            return new WeatherModel 
            { 
                City = cityName, 
                Weather = "City Not Found", 
                Temperature = 0 
            };
        }

        // Step E: Read the messy JSON into our "Net" (OpenWeatherData)
        var externalData = await response.Content.ReadFromJsonAsync<OpenWeatherData>();

        // Step F: TRANSLATION. Take messy data -> put into clean Model.
        return new WeatherModel
        {
            City = externalData.name,
            Temperature = (int)externalData.main.temp,
            Weather = externalData.weather[0].main,
            Precipitation = externalData.main.humidity
        };
    }

    // --- HELPER CLASSES (The "Net" to catch external JSON) ---
    // These must match OpenWeatherMap's format EXACTLY.
    
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
    }

    public IEnumerable<WeatherModel> GetForecast()
    {
        throw new NotImplementedException();
    }
}
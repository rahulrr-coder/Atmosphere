using Microsoft.AspNetCore.Mvc;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly IAIService _aiService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public WeatherController(
        IWeatherService weatherService, 
        IAIService aiService, 
        IConfiguration configuration, 
        HttpClient httpClient)
    {
        _weatherService = weatherService;
        _aiService = aiService;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    [HttpGet("{city}")]
    public async Task<IActionResult> GetWeather(string city)
    {
        var weather = await _weatherService.GetWeatherAsync(city);
        if (weather == null) return NotFound("City not found");
        return Ok(weather);
    }

    [HttpGet("advice")]
    public async Task<IActionResult> GetWeatherAdvice(string city)
    {
        var weather = await _weatherService.GetWeatherAsync(city);
        if (weather == null) return NotFound("City not found");

        var advice = await _aiService.GetFashionAdviceAsync(weather);
        return Ok(new { advice });
    }

    // 👇 This method caused the error before because _configuration was missing
    [HttpGet("search")]
    public async Task<IActionResult> SearchCities(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3) 
            return Ok(new List<object>()); 

        var apiKey = _configuration["OpenWeather:ApiKey"];
        // Using the direct Geo API from OpenWeather
        var url = $"http://api.openweathermap.org/geo/1.0/direct?q={query}&limit=5&appid={apiKey}";

        try 
        {
            var response = await _httpClient.GetFromJsonAsync<List<GeoResult>>(url);
            return Ok(response);
        }
        catch 
        {
            return Ok(new List<object>());
        }
    }

    // Helper class for the Search result
    public class GeoResult 
    { 
        public string name { get; set; } 
        public string country { get; set; } 
        public string state { get; set; } 
    }
}
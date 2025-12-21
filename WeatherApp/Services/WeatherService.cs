using System.Net.Http.Json;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenWeather:ApiKey"] ?? throw new Exception("API Key Missing");
    }

    public async Task<WeatherModel?> GetWeatherAsync(string city)
    {
        try 
        {
            // 1. Fetch Current + Forecast
            var currentUrl = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric";
            var forecastUrl = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={_apiKey}&units=metric";

            var currentTask = _httpClient.GetFromJsonAsync<OpenWeatherCurrent>(currentUrl);
            var forecastTask = _httpClient.GetFromJsonAsync<OpenWeatherForecast>(forecastUrl);

            await Task.WhenAll(currentTask, forecastTask);

            var currentRes = currentTask.Result;
            var forecastRes = forecastTask.Result;

            if (currentRes == null || forecastRes == null) return null;

            // 2. Fetch AQI
            int aqiLevel = 1;
            try {
                var aqiUrl = $"https://api.openweathermap.org/data/2.5/air_pollution?lat={currentRes.coord.lat}&lon={currentRes.coord.lon}&appid={_apiKey}";
                var aqiRes = await _httpClient.GetFromJsonAsync<AirPollutionResponse>(aqiUrl);
                aqiLevel = aqiRes?.list?.FirstOrDefault()?.main?.aqi ?? 1;
            } catch { }

            // 3. Map Data
            var next24h = forecastRes.list.Take(8).ToList();

            var model = new WeatherModel
            {
                City = currentRes.name,
                Country = currentRes.sys.country,
                CurrentTemp = currentRes.main.temp,
                CurrentCondition = currentRes.weather.FirstOrDefault()?.main ?? "Clear",
                Description = currentRes.weather.FirstOrDefault()?.description ?? "Clear",
                Humidity = currentRes.main.humidity,
                WindSpeed = currentRes.wind.speed,
                AQI = aqiLevel,
                MaxTemp = next24h.Any() ? next24h.Max(x => x.main.temp_max) : currentRes.main.temp_max,
                MinTemp = next24h.Any() ? next24h.Min(x => x.main.temp_min) : currentRes.main.temp_min,
                // New Mappings
                Visibility = currentRes.visibility / 1000.0, // Meters to Km
            };

            // Calculate Sun Times
            var offset = TimeSpan.FromSeconds(currentRes.timezone);
            var riseTime = DateTimeOffset.FromUnixTimeSeconds(currentRes.sys.sunrise).ToOffset(offset);
            var setTime = DateTimeOffset.FromUnixTimeSeconds(currentRes.sys.sunset).ToOffset(offset);

            model.Sunrise = riseTime.ToString("h:mm tt");
            model.Sunset = setTime.ToString("h:mm tt");
            
            var diff = setTime - riseTime;
            model.DayLength = $"{diff.Hours}h {diff.Minutes}m";

            // 4. Create the "Story"
            if (next24h.Count >= 5)
            {
                model.DayParts.Add(new DayPartForecast { PartName = "Morning", Temp = next24h[0].main.temp, Condition = next24h[0].weather[0].main });
                model.DayParts.Add(new DayPartForecast { PartName = "Afternoon", Temp = next24h[2].main.temp, Condition = next24h[2].weather[0].main });
                model.DayParts.Add(new DayPartForecast { PartName = "Evening", Temp = next24h[4].main.temp, Condition = next24h[4].weather[0].main });
            }

            return model;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
}

// Updated Classes
public class OpenWeatherCurrent { 
    public string name { get; set; } = ""; 
    public MainData main { get; set; } = new(); 
    public List<WeatherInfo> weather { get; set; } = new(); 
    public WindData wind { get; set; } = new(); 
    public Coord coord { get; set; } = new(); 
    public SysData sys { get; set; } = new(); 
    public int visibility { get; set; } 
    public int timezone { get; set; }   
}
public class OpenWeatherForecast { public List<ForecastItem> list { get; set; } = new(); }
public class ForecastItem { public MainData main { get; set; } = new(); public List<WeatherInfo> weather { get; set; } = new(); }
public class MainData { public double temp { get; set; } public int humidity { get; set; } public double temp_min { get; set; } public double temp_max { get; set; } }
public class WeatherInfo { public string main { get; set; } = ""; public string description { get; set; } = ""; }
public class WindData { public double speed { get; set; } }
public class Coord { public double lat { get; set; } public double lon { get; set; } }
public class AirPollutionResponse { public List<PollutionData> list { get; set; } = new(); }
public class PollutionData { public MainAqi main { get; set; } = new(); }
public class MainAqi { public int aqi { get; set; } }
public class SysData { 
    public string country { get; set; } = ""; 
    public long sunrise { get; set; } 
    public long sunset { get; set; }  
}
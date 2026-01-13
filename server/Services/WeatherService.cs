using System.Net.Http.Json;
using WeatherApp.Models;
using Microsoft.Extensions.Caching.Memory;

namespace WeatherApp.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;

    // Track API call count for monitoring
    private static int _apiCallsToday = 0;
    private static DateTime _lastResetDate = DateTime.UtcNow.Date;
    private const int MAX_DAILY_CALLS = 900; // Leave buffer from 1000 OpenWeather limit

    public WeatherService(
        HttpClient httpClient, 
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenWeather:ApiKey"] ?? throw new Exception("API Key Missing");
        _cache = cache;
        _logger = logger;
    }

    public async Task<WeatherModel?> GetWeatherAsync(string city)
    {
        // Reset daily counter at midnight
        if (DateTime.UtcNow.Date > _lastResetDate)
        {
            _apiCallsToday = 0;
            _lastResetDate = DateTime.UtcNow.Date;
            _logger.LogInformation("🔄 OpenWeather API call counter reset for new day");
        }

        // Create cache key (case-insensitive)
        var cacheKey = $"weather_{city.ToLower()}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out WeatherModel? cachedWeather))
        {
            _logger.LogInformation("✅ Cache HIT for {City}", city);
            return cachedWeather;
        }

        _logger.LogInformation("❌ Cache MISS for {City} - Fetching from API", city);

        // Check daily API limit before making calls
        if (_apiCallsToday >= MAX_DAILY_CALLS)
        {
            _logger.LogWarning("⚠️ OpenWeather API daily limit reached ({Count}/{Max})", 
                _apiCallsToday, MAX_DAILY_CALLS);
            throw new Exception("Daily API quota exceeded. Please try again tomorrow.");
        }

        try 
        {
            // 1. Fetch Current + Forecast
            var currentUrl = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric";
            var forecastUrl = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={_apiKey}&units=metric";

            var currentTask = _httpClient.GetFromJsonAsync<OpenWeatherCurrent>(currentUrl);
            var forecastTask = _httpClient.GetFromJsonAsync<OpenWeatherForecast>(forecastUrl);

            await Task.WhenAll(currentTask, forecastTask);

            // Increment API call counter (2 calls made: current + forecast)
            _apiCallsToday += 2;
            _logger.LogInformation("📊 OpenWeather API calls today: {Count}/{Max}", 
                _apiCallsToday, MAX_DAILY_CALLS);

            var currentRes = currentTask.Result;
            var forecastRes = forecastTask.Result;

            if (currentRes == null || forecastRes == null) return null;

            // 2. Fetch AQI
            int aqiLevel = 1;
            try {
                var aqiUrl = $"https://api.openweathermap.org/data/2.5/air_pollution?lat={currentRes.coord.lat}&lon={currentRes.coord.lon}&appid={_apiKey}";
                var aqiRes = await _httpClient.GetFromJsonAsync<AirPollutionResponse>(aqiUrl);
                _apiCallsToday++; // Increment for AQI call
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

            // Store in cache with 5 minute absolute expiration
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)) // Expire after 5 min
                .SetSize(1) // For size limit tracking
                .SetPriority(CacheItemPriority.Normal)
                .RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    _logger.LogDebug("Cache evicted: {Key}, Reason: {Reason}", key, reason);
                });

            _cache.Set(cacheKey, model, cacheOptions);
            _logger.LogInformation("💾 Cached weather data for {City} (5 min expiration)", city);

            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error fetching weather for {City}: {Message}", city, ex.Message);
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
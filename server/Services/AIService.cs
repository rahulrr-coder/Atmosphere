using System.Text.Json;
using WeatherApp.Models;
using WeatherApp.Services.AI;
using Microsoft.Extensions.Caching.Memory;

namespace WeatherApp.Services;

public interface IAIService { Task<string> GetFashionAdviceAsync(WeatherModel weather); }

public class AIService : IAIService
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly ILogger<AIService> _logger;
    private readonly IPromptService _promptService;
    private readonly IMemoryCache _cache;

    public AIService(
        IEnumerable<IAIProvider> providers, 
        ILogger<AIService> logger, 
        IPromptService promptService,
        IMemoryCache cache)
    {
        _providers = providers;
        _logger = logger;
        _promptService = promptService;
        _cache = cache;
    }

    public async Task<string> GetFashionAdviceAsync(WeatherModel weather)
    {
        // Create cache key based on city and current conditions, bucket temperature to 5°F increments
        var tempBucket = System.Math.Round(weather.CurrentTemp / 5.0) * 5.0;
        var cacheKey = $"ai_advice_{weather.City.ToLower()}_{weather.CurrentCondition}_{tempBucket:F0}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedAdvice))
        {
            _logger.LogInformation("✅ Cache HIT for AI advice: {City}", weather.City);
            return cachedAdvice;
        }

        _logger.LogInformation("❌ Cache MISS for AI advice: {City} - Generating new insights", weather.City);

        // Load prompt from template service
        var prompt = await _promptService.BuildWeatherPromptAsync(weather);

        foreach (var provider in _providers)
        {
            try
            {
                _logger.LogInformation("🤖 Trying Provider: {Name}...", provider.Name);
                
                var result = await provider.GetWeatherInsightAsync(weather, prompt);

                if (string.IsNullOrWhiteSpace(result)) 
                {
                    continue;
                }

                var cleanedJson = ExtractJson(result);
                JsonDocument.Parse(cleanedJson); // Validate
                
                // Store in cache with 10 minute expiration (AI insights change less frequently)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetSize(1)
                    .SetPriority(CacheItemPriority.Normal);

                _cache.Set(cacheKey, cleanedJson, cacheOptions);
                _logger.LogInformation("💾 Cached AI advice for {City} (10 min expiration)", weather.City);
                
                return cleanedJson;
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ {Name} Failed: {Message}", provider.Name, ex.Message);
            }
        }

        // Fallback (also cache this to avoid repeated AI calls for failures)
        var fallbackResponse = JsonSerializer.Serialize(new { 
            summary = $"Enjoy the atmosphere in {weather.City}.", 
            outfit = "Wear comfortable clothes suitable for the weather.", 
            safety = "No specific hazards." 
        });
        
        // Cache fallback for shorter duration (2 minutes)
        _cache.Set(cacheKey, fallbackResponse, TimeSpan.FromMinutes(2));
        
        return fallbackResponse;
    }

    private string ExtractJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return "{}";
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start >= 0 && end > start) return text.Substring(start, end - start + 1);
        return text.Trim(); 
    }
}
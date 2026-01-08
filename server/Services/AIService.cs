using System.Text.Json;
using WeatherApp.Models;
using WeatherApp.Services.AI;

namespace WeatherApp.Services;

public interface IAIService { Task<string> GetFashionAdviceAsync(WeatherModel weather); }

public class AIService : IAIService
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly ILogger<AIService> _logger;
    private readonly IPromptService _promptService;

    public AIService(IEnumerable<IAIProvider> providers, ILogger<AIService> logger, IPromptService promptService)
    {
        _providers = providers;
        _logger = logger;
        _promptService = promptService;
    }

    public async Task<string> GetFashionAdviceAsync(WeatherModel weather)
    {
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
                
                return cleanedJson;
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ {Name} Failed: {Message}", provider.Name, ex.Message);
            }
        }

        // Fallback
        return JsonSerializer.Serialize(new { 
            summary = $"Enjoy the atmosphere in {weather.City}.", 
            outfit = "Wear comfortable clothes suitable for the weather.", 
            safety = "No specific hazards." 
        });
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
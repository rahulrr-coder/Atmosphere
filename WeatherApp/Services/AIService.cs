using System.Text.Json;
using WeatherApp.Models;
using WeatherApp.Services.AI;

namespace WeatherApp.Services;

public interface IAIService { Task<string> GetFashionAdviceAsync(WeatherModel weather); }

public class AIService : IAIService
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly ILogger<AIService> _logger;

    public AIService(IEnumerable<IAIProvider> providers, ILogger<AIService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<string> GetFashionAdviceAsync(WeatherModel weather)
    {
        // 👇 UPDATED PROMPT: More relatable, less "high-fashion"
        var prompt = $@"
            Role: You are a smart, practical style companion who gives helpful daily advice.
            Context: {weather.City}, {weather.Country}.
            Data: Temp {weather.CurrentTemp:F0}°C, {weather.CurrentCondition}. Humidity {weather.Humidity}%. Wind {weather.WindSpeed}m/s. AQI {weather.AQI}.
            
            Task: Return a FLAT JSON object (no nesting).
            
            Guidelines:
            - 'summary': A warm, human-like summary of the weather feel (max 2 sentences).
            - 'outfit': Suggest comfortable, smart-casual, or streetwear options suitable for daily life. Avoid overly luxurious items like 'cashmere' or 'trench coats' unless strictly necessary for extreme cold.
            - 'safety': Practical tips. 
               * IF Rain/Drizzle -> Suggest Umbrella/Raincoat.
               * IF AQI > 150 -> Suggest a Mask.
               * IF Clear/Sunny -> Suggest Sunscreen/Sunglasses.
               * ELSE -> 'No specific hazards.'
            
            Example Output:
            {{
                ""summary"": ""It's a warm and humid day, so stick to breathable fabrics."",
                ""outfit"": ""Cotton tee with lightweight trousers or shorts."",
                ""safety"": ""Stay hydrated and seek shade.""
            }}
        ";

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
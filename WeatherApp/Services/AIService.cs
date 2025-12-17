using System.Text.Json;
using WeatherApp.Models;
using WeatherApp.Services.AI;

namespace WeatherApp.Services;

public interface IAIService { Task<string> GetFashionAdviceAsync(WeatherModel weather); }

public class AIService : IAIService
{
    private readonly IEnumerable<IAIProvider> _providers;

    // DI injects ALL registered providers
    public AIService(IEnumerable<IAIProvider> providers)
    {
        _providers = providers;
    }

    public async Task<string> GetFashionAdviceAsync(WeatherModel weather)
    {
       var prompt = $@"
        You are a high-end fashion and lifestyle editor.
        Context: {weather.City}, {weather.Country}, {weather.CurrentTemp:F0}°C, {weather.CurrentCondition}.
        
        Task: Return a FLAT JSON object with exactly these 3 keys.
        
        Rules:
        1. 'summary': A warm, engaging briefing (max 2 sentences). No boring stats.
        2. 'outfit': A stylish clothing recommendation.
        3. 'safety': A practical tip (e.g., 'Carry an umbrella', 'Wear sunscreen', or 'No hazards').
        
        IMPORTANT: Do not nest objects. Values must be simple strings.
        
        Example Output:
        {{
            ""summary"": ""London is calling with a misty morning, but the sun might peek through later."",
            ""outfit"": ""Trench coat over a merino wool sweater and leather boots."",
            ""safety"": ""Roads might be slick, watch your step.""
        }}
    ";

        foreach (var provider in _providers)
        {
            Console.WriteLine($"🤖 Trying Provider: {provider.Name}...");
            var result = await provider.GetWeatherInsightAsync(weather, prompt);

            if (!string.IsNullOrWhiteSpace(result))
            {
                return CleanJson(result);
            }
        }

        // Ultimate Fallback
        return JsonSerializer.Serialize(new { 
            summary = $"Enjoy the weather in {weather.City}.", 
            outfit = "Wear comfortable clothes.", 
            safety = "Stay safe." 
        });
    }

    private string CleanJson(string raw) => raw.Replace("```json", "").Replace("```", "").Trim();
}
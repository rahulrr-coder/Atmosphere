using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherApp.Models;

namespace WeatherApp.Services.AI;

public class GeminiProvider : IAIProvider
{
    public string Name => "Gemini (Multi-Model)";
    private readonly HttpClient _http;
    private readonly string _apiKey;

    // 🧠 INTERNAL FALLBACK LIST
    private readonly string[] _models = { 
        "gemini-2.0-flash-exp",   // Try Latest
        "gemini-2.5-flash-lite",  // Try Lite (New Free Tier Friendly)
        "gemini-1.5-flash"        // Old Reliable
    };

    public GeminiProvider(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["AI:GeminiKey"] ?? "";
    }

    public async Task<string?> GetWeatherInsightAsync(WeatherModel w, string systemPrompt)
    {
        if (string.IsNullOrEmpty(_apiKey)) return null;

        var requestBody = new { contents = new[] { new { parts = new[] { new { text = systemPrompt } } } } };
        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        // Loop through models until one works
        foreach (var model in _models)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
            
            try
            {
                var res = await _http.PostAsync(url, jsonContent);
                if (res.IsSuccessStatusCode)
                {
                    Console.WriteLine($"🟢 Gemini Success using: {model}");
                    var json = await res.Content.ReadAsStringAsync();
                    var obj = JsonSerializer.Deserialize<GeminiResponse>(json);
                    return obj?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                }
                else
                {
                    Console.WriteLine($"⚠️ Gemini {model} Failed: {res.StatusCode} (Trying next...)");
                }
            }
            catch { /* Network error, try next */ }
        }

        Console.WriteLine("🔴 All Gemini models exhausted.");
        return null;
    }

    // Helper classes for Gemini
    private class GeminiResponse { [JsonPropertyName("candidates")] public List<Candidate>? Candidates { get; set; } }
    private class Candidate { [JsonPropertyName("content")] public Content? Content { get; set; } }
    private class Content { [JsonPropertyName("parts")] public List<Part>? Parts { get; set; } }
    private class Part { [JsonPropertyName("text")] public string? Text { get; set; } }
}
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherApp.Models; // Needed for WeatherModel

namespace WeatherApp.Services;

public interface IAIService
{
    // Updated signature to accept the full WeatherModel for the "Story" logic
    Task<string> GetFashionAdviceAsync(WeatherModel weather);
}

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetFashionAdviceAsync(WeatherModel weather)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return "{}"; // Return empty JSON if key missing

        // 1. Build a textual summary of the forecast parts (Morning/Afternoon/Evening)
        // This lets the AI "see the future" to write the story
        var forecastSummary = string.Join(", ", weather.DayParts.Select(p => $"{p.PartName}: {p.Temp:F0}°C {p.Condition}"));

        // 2. The New "Storyteller" Prompt
        var prompt = $@"
            Context: Current weather in {weather.City} is {weather.CurrentTemp:F0}°C ({weather.CurrentCondition}). 
            Forecast segments: {forecastSummary}.
            Humidity: {weather.Humidity}%. Wind: {weather.WindSpeed} m/s. AQI: {weather.AQI}.

            Task: Act as a witty weather lifestyle assistant. Return a VALID JSON object.
            Do NOT use Markdown formatting (no ```json blocks). Just return the raw JSON string.
            
            JSON Structure & Keys:
            1. 'headline': A catchy, 5-7 word hook summarizing the day (e.g., 'Sunny start with a breezy picnic evening!').
            2. 'story': A 2-sentence narrative telling the user how the day evolves based on the forecast segments.
            3. 'outfit': Casual, friendly clothing advice.
            4. 'vibe': A fun 'Lifestyle Index' (e.g., 'Laundry Day', 'Kite Flying', 'Netflix & Chill', 'Good Hair Day').

            Example JSON:
            {{
                ""headline"": ""Perfect sunny morning, but grab an umbrella for 4 PM!"",
                ""story"": ""Start your day light, but watch out for the evening breeze. Clouds roll in later, making it perfect for a cozy dinner."",
                ""outfit"": ""T-shirt now, hoodie later."",
                ""vibe"": ""Picnic Perfect""
            }}
        ";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Using gemini-1.5-flash for speed and reliability
        var url = $"[https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=](https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=){apiKey}";
        
        try 
        {
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GeminiResponse>(responseString);
            
            // Return the text directly. The Frontend will parse the JSON.
            return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "{}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI Error: {ex.Message}");
            return "{}"; // Return empty JSON on failure
        }
    }
}

// --- Helper Classes to map Gemini's API Response ---
public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; set; }
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WeatherApp.Models;

namespace WeatherApp.Services.AI;

public class MistralProvider : IAIProvider
{
    public string Name => "Mistral AI";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public MistralProvider(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["AI:MistralKey"] ?? "";
    }

    public async Task<string?> GetWeatherInsightAsync(WeatherModel weather, string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey)) return ""; // Fail silently so the next provider takes over

        var requestBody = new
        {
            model = "mistral-tiny", // or "mistral-small"
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _apiKey);

        try 
        {
            var response = await _httpClient.PostAsync("https://api.mistral.ai/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var resJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resJson);
            
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Mistral AI failed: {ex.Message}");
            return null; // Return null to let AIService try the next provider
        }
    }
}
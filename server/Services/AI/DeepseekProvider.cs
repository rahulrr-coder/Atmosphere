using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WeatherApp.Models;

namespace WeatherApp.Services.AI;

public class DeepSeekProvider : IAIProvider
{
    public string Name => "DeepSeek";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public DeepSeekProvider(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["AI:DeepSeekKey"] ?? "";
    }

    public async Task<string?> GetWeatherInsightAsync(WeatherModel weather, string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey)) return "";

        var requestBody = new
        {
            model = "deepseek-chat", 
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
            var response = await _httpClient.PostAsync("https://api.deepseek.com/chat/completions", content);
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
            Console.WriteLine($"⚠️ DeepSeek AI failed: {ex.Message}");
            return null;
        }
    }
}
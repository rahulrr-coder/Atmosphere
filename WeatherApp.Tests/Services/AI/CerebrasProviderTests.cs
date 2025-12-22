using Moq;
using Moq.Protected;
using System.Net;
using WeatherApp.Models;
using WeatherApp.Services.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WeatherApp.Tests.Services.AI;

public class CerebrasProviderTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly CerebrasProvider _provider;

    public CerebrasProviderTests()
    {
        // 1. Setup Mock Config - Ensure key matches what Provider looks for
        _mockConfig = new Mock<IConfiguration>();
        // 🟢 FIX: Changed "AI:CerebrasApiKey" to "AI:CerebrasKey" to match your pattern
        _mockConfig.Setup(c => c["AI:CerebrasKey"]).Returns("fake_cerebras_key");

        // 2. Setup Mock HttpClient
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpHandler.Object);

        // 3. Initialize Provider
        _provider = new CerebrasProvider(httpClient, _mockConfig.Object);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnText_WhenApiSucceeds()
    {
        // Arrange
        // We use the standard OpenAI/Cerebras JSON structure
        var fakeResponse = @"{
            ""choices"": [
                {
                    ""message"": { ""content"": ""Take an umbrella."" }
                }
            ]
        }";

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeResponse)
            });

        // Act
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel { City = "RainyCity" }, "prompt");

        // Assert
        Assert.NotNull(result); 
        Assert.Equal("Take an umbrella.", result);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnNull_WhenApiFails()
    {
        // Arrange: Simulate a 401 Unauthorized error
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized });

        // Act
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel(), "prompt");

        // Assert
        Assert.Null(result);
    }
}
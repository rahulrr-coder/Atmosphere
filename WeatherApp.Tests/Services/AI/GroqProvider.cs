using Moq;
using Moq.Protected;
using System.Net;
using WeatherApp.Models;
using WeatherApp.Services.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WeatherApp.Tests.Services.AI;

public class GroqProviderTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly GroqProvider _provider;

    public GroqProviderTests()
    {
        // 1. Setup Mock Config
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["AI:GroqKey"]).Returns("fake_groq_key");

        // 2. Setup Mock HttpClient
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpHandler.Object);

        // 3. Initialize Provider
        _provider = new GroqProvider(httpClient, _mockConfig.Object);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnText_WhenGroqResponds()
    {
        // Arrange
        var fakeResponse = @"{
            ""choices"": [{
                ""message"": { ""content"": ""Groq Suggestion"" }
            }]
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
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel(), "test prompt");

        // Assert
        Assert.Equal("Groq Suggestion", result);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnNull_WhenApiFails()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized });

        // Act
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel(), "test prompt");

        // Assert
        Assert.Null(result);
    }
}
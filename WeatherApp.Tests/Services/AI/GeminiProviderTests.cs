using Moq;
using Moq.Protected;
using System.Net;
using WeatherApp.Models;
using WeatherApp.Services.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WeatherApp.Tests.Services.AI;

public class GeminiProviderTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly GeminiProvider _provider;

    public GeminiProviderTests()
    {
        // 1. Setup Mock Config
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["AI:GeminiKey"]).Returns("fake_gemini_key");

        // 2. Setup Mock HttpClient
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpHandler.Object);

        // 3. Initialize Provider (Constructor name MUST match class name)
        _provider = new GeminiProvider(httpClient, _mockConfig.Object);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnText_WhenGeminiResponds()
    {
        // Arrange
        var fakeResponse = @"{
            ""candidates"": [{
                ""content"": {
                    ""parts"": [{ ""text"": ""Gemini Suggestion"" }]
                }
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
        Assert.Equal("Gemini Suggestion", result);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnNull_WhenAllModelsFail()
    {
        // Arrange: Force a 500 error for all attempts
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel(), "test prompt");

        // Assert
        Assert.Null(result);
    }
}   
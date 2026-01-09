using Moq;
using Moq.Protected;
using System.Net;
using WeatherApp.Models;
using WeatherApp.Services.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WeatherApp.Tests.Services.AI;

public class DeepSeekProviderTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly DeepSeekProvider _provider;

    public DeepSeekProviderTests()
    {
        // Setup Mock Config
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["AI:DeepSeekKey"]).Returns("fake_deepseek_key");

        // Setup Mock HttpClient
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpHandler.Object);

        // Initialize Provider
        _provider = new DeepSeekProvider(httpClient, _mockConfig.Object);
    }

    [Fact]
    public void Name_ShouldReturnDeepSeek()
    {
        // Assert
        Assert.Equal("DeepSeek", _provider.Name);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnText_WhenApiSucceeds()
    {
        // Arrange
        var fakeResponse = @"{
            ""choices"": [
                {
                    ""message"": { ""content"": ""Weather looks good today!"" }
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
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel { City = "Tokyo" }, "test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Weather looks good today!", result);
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
            .ReturnsAsync(new HttpResponseMessage 
            { 
                StatusCode = HttpStatusCode.InternalServerError 
            });

        // Act
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel(), "test prompt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnEmptyString_WhenApiKeyIsMissing()
    {
        // Arrange
        var mockConfigNoKey = new Mock<IConfiguration>();
        mockConfigNoKey.Setup(c => c["AI:DeepSeekKey"]).Returns((string?)null);

        var httpClient = new HttpClient(_mockHttpHandler.Object);
        var providerNoKey = new DeepSeekProvider(httpClient, mockConfigNoKey.Object);

        // Act
        var result = await providerNoKey.GetWeatherInsightAsync(new WeatherModel(), "test prompt");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnNull_WhenUnauthorized()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage 
            { 
                StatusCode = HttpStatusCode.Unauthorized 
            });

        // Act
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel(), "test prompt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldUseCorrectEndpoint()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""choices"":[{""message"":{""content"":""test""}}]}")
            });

        // Act
        await _provider.GetWeatherInsightAsync(new WeatherModel(), "test prompt");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://api.deepseek.com/chat/completions", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }
}

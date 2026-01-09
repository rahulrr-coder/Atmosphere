using Moq;
using Moq.Protected;
using System.Net;
using WeatherApp.Models;
using WeatherApp.Services.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WeatherApp.Tests.Services.AI;

public class MistralProviderTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly MistralProvider _provider;

    public MistralProviderTests()
    {
        // Setup Mock Config
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["AI:MistralKey"]).Returns("fake_mistral_key");

        // Setup Mock HttpClient
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpHandler.Object);

        // Initialize Provider
        _provider = new MistralProvider(httpClient, _mockConfig.Object);
    }

    [Fact]
    public void Name_ShouldReturnMistralAI()
    {
        // Assert
        Assert.Equal("Mistral AI", _provider.Name);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnText_WhenApiSucceeds()
    {
        // Arrange
        var fakeResponse = @"{
            ""choices"": [
                {
                    ""message"": { ""content"": ""Expect rain this afternoon."" }
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
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel { City = "Paris" }, "test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Expect rain this afternoon.", result);
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
                StatusCode = HttpStatusCode.BadRequest 
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
        mockConfigNoKey.Setup(c => c["AI:MistralKey"]).Returns((string?)null);

        var httpClient = new HttpClient(_mockHttpHandler.Object);
        var providerNoKey = new MistralProvider(httpClient, mockConfigNoKey.Object);

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
        Assert.Equal("https://api.mistral.ai/v1/chat/completions", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }

    [Fact]
    public async Task GetWeatherInsightAsync_ShouldReturnNull_WhenExceptionOccurs()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _provider.GetWeatherInsightAsync(new WeatherModel(), "test prompt");

        // Assert
        Assert.Null(result);
    }
}

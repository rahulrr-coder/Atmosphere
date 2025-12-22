using Moq;
using Xunit;
using WeatherApp.Services;
using WeatherApp.Services.AI;
using WeatherApp.Models;
using Microsoft.Extensions.Logging;

namespace WeatherApp.Tests.Services;

public class AIServiceTests
{
    private readonly Mock<ILogger<AIService>> _mockLogger;

    public AIServiceTests()
    {
        _mockLogger = new Mock<ILogger<AIService>>();
    }

    [Fact]
    public async Task GetFashionAdviceAsync_ShouldReturnFirstSuccess()
    {
        // Arrange
        var weather = new WeatherModel { City = "Paris" };
        
        // Provider 1: Fails
        var mockProvider1 = new Mock<IAIProvider>();
        mockProvider1.Setup(p => p.Name).Returns("Provider1");
        mockProvider1.Setup(p => p.GetWeatherInsightAsync(It.IsAny<WeatherModel>(), It.IsAny<string>()))
                     .ThrowsAsync(new Exception("API Error"));

        // Provider 2: Succeeds
        var mockProvider2 = new Mock<IAIProvider>();
        mockProvider2.Setup(p => p.Name).Returns("Provider2");
        mockProvider2.Setup(p => p.GetWeatherInsightAsync(It.IsAny<WeatherModel>(), It.IsAny<string>()))
                     .ReturnsAsync("{\"summary\": \"Success!\"}");

        var providers = new List<IAIProvider> { mockProvider1.Object, mockProvider2.Object };
        var service = new AIService(providers, _mockLogger.Object);

        // Act
        var result = await service.GetFashionAdviceAsync(weather);

        // Assert
        Assert.Contains("Success!", result);
        // Verify provider 2 was actually called
        mockProvider2.Verify(p => p.GetWeatherInsightAsync(It.IsAny<WeatherModel>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetFashionAdviceAsync_ShouldReturnFallback_WhenAllFail()
    {
        // Arrange
        var weather = new WeatherModel { City = "Mars" };
        var mockProvider1 = new Mock<IAIProvider>();
        mockProvider1.Setup(p => p.GetWeatherInsightAsync(It.IsAny<WeatherModel>(), It.IsAny<string>()))
                     .ThrowsAsync(new Exception("Fail"));

        var providers = new List<IAIProvider> { mockProvider1.Object };
        var service = new AIService(providers, _mockLogger.Object);

        // Act
        var result = await service.GetFashionAdviceAsync(weather);

        // Assert
        // Check for the hardcoded fallback text from your AIService.cs
        Assert.Contains("Enjoy the atmosphere", result); 
    }
}
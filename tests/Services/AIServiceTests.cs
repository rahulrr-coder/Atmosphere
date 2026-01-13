using Moq;
using Xunit;
using WeatherApp.Services;
using WeatherApp.Services.AI;
using WeatherApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace WeatherApp.Tests.Services;

public class AIServiceTests
{
    private readonly Mock<ILogger<AIService>> _mockLogger;
    private readonly Mock<IPromptService> _mockPromptService;
    private readonly IMemoryCache _cache;

    public AIServiceTests()
    {
        _mockLogger = new Mock<ILogger<AIService>>();
        _mockPromptService = new Mock<IPromptService>();
        _mockPromptService.Setup(p => p.BuildWeatherPromptAsync(It.IsAny<WeatherModel>()))
            .ReturnsAsync("Test weather prompt");
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task GetFashionAdviceAsync_ShouldReturnFirstSuccess()
    {
        // Arrange
        var weather = new WeatherModel { City = "Paris", CurrentCondition = "Clear", CurrentTemp = 20 };
        
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
        var service = new AIService(providers, _mockLogger.Object, _mockPromptService.Object, _cache);

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
        var weather = new WeatherModel { City = "Mars", CurrentCondition = "Unknown", CurrentTemp = -50 };
        var mockProvider1 = new Mock<IAIProvider>();
        mockProvider1.Setup(p => p.GetWeatherInsightAsync(It.IsAny<WeatherModel>(), It.IsAny<string>()))
                     .ThrowsAsync(new Exception("Fail"));

        var providers = new List<IAIProvider> { mockProvider1.Object };
        var service = new AIService(providers, _mockLogger.Object, _mockPromptService.Object, _cache);

        // Act
        var result = await service.GetFashionAdviceAsync(weather);

        // Assert
        // Check for the hardcoded fallback text from your AIService.cs
        Assert.Contains("Enjoy the atmosphere", result); 
    }

    [Fact]
    public async Task GetFashionAdviceAsync_ShouldReturnCachedResult_OnSecondCall()
    {
        // Arrange
        var weather = new WeatherModel { City = "Tokyo", CurrentCondition = "Rainy", CurrentTemp = 18 };
        
        int providerCallCount = 0;
        var mockProvider = new Mock<IAIProvider>();
        mockProvider.Setup(p => p.Name).Returns("TestProvider");
        mockProvider.Setup(p => p.GetWeatherInsightAsync(It.IsAny<WeatherModel>(), It.IsAny<string>()))
                    .ReturnsAsync(() => 
                    {
                        providerCallCount++;
                        return "{\"summary\": \"Rainy day advice\"}";
                    });

        var providers = new List<IAIProvider> { mockProvider.Object };
        var service = new AIService(providers, _mockLogger.Object, _mockPromptService.Object, _cache);

        // Act
        var result1 = await service.GetFashionAdviceAsync(weather);
        var result2 = await service.GetFashionAdviceAsync(weather);

        // Assert
        Assert.Contains("Rainy day advice", result1);
        Assert.Contains("Rainy day advice", result2);
        
        // Provider should only be called once (second call uses cache)
        Assert.Equal(1, providerCallCount);
        mockProvider.Verify(p => p.GetWeatherInsightAsync(It.IsAny<WeatherModel>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetFashionAdviceAsync_ShouldUseDifferentCacheKeys_ForDifferentConditions()
    {
        // Arrange
        var weatherSunny = new WeatherModel { City = "Miami", CurrentCondition = "Clear", CurrentTemp = 30 };
        var weatherRainy = new WeatherModel { City = "Miami", CurrentCondition = "Rain", CurrentTemp = 20 };
        
        int providerCallCount = 0;
        var mockProvider = new Mock<IAIProvider>();
        mockProvider.Setup(p => p.Name).Returns("TestProvider");
        mockProvider.Setup(p => p.GetWeatherInsightAsync(It.IsAny<WeatherModel>(), It.IsAny<string>()))
                    .ReturnsAsync((WeatherModel w, string prompt) => 
                    {
                        providerCallCount++;
                        return $"{{\"summary\": \"{w.CurrentCondition} advice\"}}";
                    });

        var providers = new List<IAIProvider> { mockProvider.Object };
        var service = new AIService(providers, _mockLogger.Object, _mockPromptService.Object, _cache);

        // Act
        var resultSunny = await service.GetFashionAdviceAsync(weatherSunny);
        var resultRainy = await service.GetFashionAdviceAsync(weatherRainy);

        // Assert
        Assert.Contains("Clear advice", resultSunny);
        Assert.Contains("Rain advice", resultRainy);
        
        // Should make 2 API calls (different cache keys)
        Assert.Equal(2, providerCallCount);
    }
}

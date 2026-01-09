using Microsoft.AspNetCore.Hosting;
using Moq;
using WeatherApp.Models;
using WeatherApp.Services;
using Xunit;

namespace WeatherApp.Tests.Services;

public class PromptServiceTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly PromptService _service;
    private readonly string _testTemplatePath;

    public PromptServiceTests()
    {
        _mockEnv = new Mock<IWebHostEnvironment>();
        _testTemplatePath = Path.Combine(Path.GetTempPath(), "AIPrompt.txt");
        
        // Mock the ContentRootPath to use temp directory
        _mockEnv.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());
        
        _service = new PromptService(_mockEnv.Object);
    }

    [Fact]
    public async Task BuildWeatherPromptAsync_ShouldReplaceAllPlaceholders_WhenTemplateExists()
    {
        // Arrange
        var templateContent = "City: {{City}}, Country: {{Country}}, Temp: {{Temp}}°C, " +
                            "Condition: {{Condition}}, Humidity: {{Humidity}}%, " +
                            "Wind: {{Wind}} km/h, AQI: {{AQI}}";
        
        var templatesDir = Path.Combine(Path.GetTempPath(), "Templates");
        Directory.CreateDirectory(templatesDir);
        await File.WriteAllTextAsync(Path.Combine(templatesDir, "AIPrompt.txt"), templateContent);

        var weather = new WeatherModel
        {
            City = "Toronto",
            Country = "Canada",
            CurrentTemp = 25.5,
            CurrentCondition = "Sunny",
            Humidity = 65,
            WindSpeed = 12.3,
            AQI = 42
        };

        // Act
        var result = await _service.BuildWeatherPromptAsync(weather);

        // Assert
        Assert.Contains("City: Toronto", result);
        Assert.Contains("Country: Canada", result);
        Assert.Contains("Temp: 25°C", result);
        Assert.Contains("Condition: Sunny", result);
        Assert.Contains("Humidity: 65%", result);
        Assert.Contains("Wind: 12.3 km/h", result);
        Assert.Contains("AQI: 42", result);

        // Cleanup
        Directory.Delete(templatesDir, true);
    }

    [Fact]
    public async Task BuildWeatherPromptAsync_ShouldUseFallback_WhenTemplateDoesNotExist()
    {
        // Arrange
        var weather = new WeatherModel
        {
            City = "London",
            Country = "UK",
            CurrentTemp = 15.0,
            CurrentCondition = "Rainy",
            Humidity = 80,
            WindSpeed = 20.5,
            AQI = 55
        };

        // Act
        var result = await _service.BuildWeatherPromptAsync(weather);

        // Assert - Should use fallback template
        Assert.Contains("London", result);
        Assert.Contains("15°C", result);
    }

    [Fact]
    public async Task BuildWeatherPromptAsync_ShouldCacheTemplate_AndReuseIt()
    {
        // Arrange
        var templateContent = "Weather for {{City}}: {{Temp}}°C";
        var templatesDir = Path.Combine(Path.GetTempPath(), "Templates");
        Directory.CreateDirectory(templatesDir);
        var templatePath = Path.Combine(templatesDir, "AIPrompt.txt");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var weather1 = new WeatherModel { City = "Paris", CurrentTemp = 20 };
        var weather2 = new WeatherModel { City = "Berlin", CurrentTemp = 18 };

        // Act
        var result1 = await _service.BuildWeatherPromptAsync(weather1);
        
        // Delete template after first call to verify caching
        File.Delete(templatePath);
        
        var result2 = await _service.BuildWeatherPromptAsync(weather2);

        // Assert - Both should work because template was cached
        Assert.Contains("Paris", result1);
        Assert.Contains("20°C", result1);
        Assert.Contains("Berlin", result2);
        Assert.Contains("18°C", result2);

        // Cleanup
        Directory.Delete(templatesDir, true);
    }

    [Fact]
    public async Task BuildWeatherPromptAsync_ShouldFormatTemperature_WithoutDecimals()
    {
        // Arrange
        var templateContent = "Temperature: {{Temp}}°C";
        var templatesDir = Path.Combine(Path.GetTempPath(), "Templates");
        Directory.CreateDirectory(templatesDir);
        await File.WriteAllTextAsync(Path.Combine(templatesDir, "AIPrompt.txt"), templateContent);

        var weather = new WeatherModel { CurrentTemp = 25.8 };

        // Act
        var result = await _service.BuildWeatherPromptAsync(weather);

        // Assert - Temperature should be formatted as integer
        Assert.Contains("26°C", result);

        // Cleanup
        Directory.Delete(templatesDir, true);
    }

    [Fact]
    public async Task BuildWeatherPromptAsync_ShouldFormatWindSpeed_WithOneDecimal()
    {
        // Arrange
        var templateContent = "Wind: {{Wind}} km/h";
        var templatesDir = Path.Combine(Path.GetTempPath(), "Templates");
        Directory.CreateDirectory(templatesDir);
        await File.WriteAllTextAsync(Path.Combine(templatesDir, "AIPrompt.txt"), templateContent);

        var weather = new WeatherModel { WindSpeed = 12.345 };

        // Act
        var result = await _service.BuildWeatherPromptAsync(weather);

        // Assert - Wind speed should have 1 decimal
        Assert.Contains("12.3 km/h", result);

        // Cleanup
        Directory.Delete(templatesDir, true);
    }
}

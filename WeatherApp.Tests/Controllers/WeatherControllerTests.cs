using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using WeatherApp.Controllers;
using WeatherApp.Models;
using WeatherApp.Services;
using Xunit;

namespace WeatherApp.Tests.Controllers;

public class WeatherControllerTests
{
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly Mock<IAIService> _mockAiService;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly WeatherController _controller;

    public WeatherControllerTests()
    {
        _mockWeatherService = new Mock<IWeatherService>();
        _mockAiService = new Mock<IAIService>();
        _mockConfig = new Mock<IConfiguration>();
        
        // Setup Config for Search API Key
        _mockConfig.Setup(c => c["OpenWeather:ApiKey"]).Returns("test_key");

        // Setup Fake HTTP Handler
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);

        _controller = new WeatherController(
            _mockWeatherService.Object, 
            _mockAiService.Object, 
            _mockConfig.Object, 
            _httpClient
        );
    }

    [Fact]
    public async Task GetWeather_ShouldReturnOk_WhenCityFound()
    {
        // Arrange
        var mockWeather = new WeatherModel { City = "Dubai", CurrentTemp = 30 };
        _mockWeatherService.Setup(s => s.GetWeatherAsync("Dubai")).ReturnsAsync(mockWeather);

        // Act
        var result = await _controller.GetWeather("Dubai");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<WeatherModel>(okResult.Value);
        Assert.Equal("Dubai", data.City);
    }

    [Fact]
    public async Task GetWeather_ShouldReturnNotFound_WhenCityNull()
    {
        // Arrange
        _mockWeatherService.Setup(s => s.GetWeatherAsync("Unknown")).ReturnsAsync((WeatherModel?)null);

        // Act
        var result = await _controller.GetWeather("Unknown");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetWeatherAdvice_ShouldReturnAdvice_WhenCityExists()
    {
        // Arrange
        var mockWeather = new WeatherModel { City = "Paris" };
        _mockWeatherService.Setup(s => s.GetWeatherAsync("Paris")).ReturnsAsync(mockWeather);
        _mockAiService.Setup(s => s.GetFashionAdviceAsync(mockWeather)).ReturnsAsync("Wear a beret.");

        // Act
        var result = await _controller.GetWeatherAdvice("Paris");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        // Using dynamic here because the controller returns an anonymous object { advice = ... }
        dynamic val = okResult.Value!;
        Assert.Equal("Wear a beret.", val.GetType().GetProperty("advice")?.GetValue(val, null));
    }

    [Fact]
    public async Task SearchCities_ShouldReturnList_WhenApiCallSucceeds()
    {
        // Arrange: Mock the Geo API response
        var fakeGeoJson = "[{\"name\":\"London\",\"country\":\"GB\",\"state\":\"England\"}]";
        
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeGeoJson)
            });

        // Act
        var result = await _controller.SearchCities("Lon");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<WeatherController.GeoResult>>(okResult.Value);
        Assert.Single(list);
        Assert.Equal("London", list[0].name);
    }

    [Fact]
    public async Task SearchCities_ShouldReturnEmpty_WhenQueryTooShort()
    {
        // Act
        var result = await _controller.SearchCities("Lo"); // Too short

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<object>>(okResult.Value);
        Assert.Empty(list);
    }
}
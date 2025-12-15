using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using WeatherApp.Services;
using WeatherApp.Models;
using Xunit;

namespace WeatherApp.Tests;

public class WeatherServiceTests
{
    [Fact]
    public async Task GetWeatherForCity_ReturnsData_WhenApiIsSuccessful()
    {
        // 1. Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["OpenWeather:ApiKey"]).Returns("test_key");

        // Mock the HTTP Response (Fake Internet)
        var handlerMock = new Mock<HttpMessageHandler>();
        var fakeJson = @"{ 
            ""name"": ""Dubai"", 
            ""main"": { ""temp"": 35, ""humidity"": 40 },
            ""weather"": [ { ""main"": ""Clear"" } ],
            ""coord"": { ""lat"": 25.2, ""lon"": 55.2 }
        }";

        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(fakeJson)
           });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new WeatherService(httpClient, mockConfig.Object);

        // 2. Act
        var result = await service.GetWeatherAsync("Dubai");

        // 3. Assert
        Assert.Equal("Dubai", result.City);
        Assert.Equal(35, result.Temperature);
    }

    [Fact]
    public async Task GetWeatherForCity_ReturnsError_WhenCityNotFound()
    {
        // 1. Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["OpenWeather:ApiKey"]).Returns("test_key");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.NotFound // 404 Error
           });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new WeatherService(httpClient, mockConfig.Object);

        // 2. Act
        var result = await service.GetWeatherAsync("Unknown");

        // 3. Assert
        Assert.Equal("City Not Found", result.Weather);
    }
}
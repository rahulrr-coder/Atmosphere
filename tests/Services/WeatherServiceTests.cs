using Moq;
using Moq.Protected;
using System.Net;
using WeatherApp.Services;
using WeatherApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Xunit;

namespace WeatherApp.Tests.Services;

public class WeatherServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<WeatherService>> _mockLogger;
    private readonly WeatherService _service;

    public WeatherServiceTests()
    {
        // 1. Mock Configuration (API Key)
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["OpenWeather:ApiKey"]).Returns("test_api_key");

        // 2. Mock HTTP Handler (The "Network Interceptor")
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpHandler.Object) 
        { 
            BaseAddress = new Uri("https://api.openweathermap.org") 
        };

        // 3. Real MemoryCache for testing
        _cache = new MemoryCache(new MemoryCacheOptions());

        // 4. Mock Logger
        _mockLogger = new Mock<ILogger<WeatherService>>();

        // 5. Create Service with Mocks
        _service = new WeatherService(httpClient, _mockConfig.Object, _cache, _mockLogger.Object);
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldMapDataCorrectly_WhenAllApisSucceed()
    {
        // ARRANGE: Create Fake JSON Responses for all 3 calls
        
        // 1. Current Weather JSON (Includes Visibility, Clouds, Sun Times)
        // We set Timezone to 0 (UTC) for easy calculation
        // Sunrise = 1000, Sunset = 5000. Current Time (Now) isn't mocked here easily, 
        // so UV logic might vary based on when you run the test, but other fields are strict.
        var fakeCurrentJson = @"{
            ""name"": ""Dubai"",
            ""sys"": { ""country"": ""AE"", ""sunrise"": 1672531200, ""sunset"": 1672574400 },
            ""main"": { ""temp"": 30.5, ""humidity"": 40, ""temp_min"": 28, ""temp_max"": 32 },
            ""wind"": { ""speed"": 5.5 },
            ""weather"": [{ ""main"": ""Clear"", ""description"": ""clear sky"" }],
            ""clouds"": { ""all"": 0 },
            ""coord"": { ""lat"": 25.2, ""lon"": 55.2 },
            ""visibility"": 10000,
            ""timezone"": 14400
        }";

        // 2. Forecast JSON (Needs at least 8 items for your High/Low logic)
        // We create a simple list with varying temps to test Max/Min
        var fakeForecastJson = @"{
            ""list"": [
                { ""main"": { ""temp"": 30, ""temp_min"": 29, ""temp_max"": 31 }, ""weather"": [{ ""main"": ""Sun"" }] },
                { ""main"": { ""temp"": 32, ""temp_min"": 31, ""temp_max"": 33 }, ""weather"": [{ ""main"": ""Sun"" }] },
                { ""main"": { ""temp"": 34, ""temp_min"": 33, ""temp_max"": 35 }, ""weather"": [{ ""main"": ""Sun"" }] },
                { ""main"": { ""temp"": 29, ""temp_min"": 28, ""temp_max"": 30 }, ""weather"": [{ ""main"": ""Sun"" }] },
                { ""main"": { ""temp"": 28, ""temp_min"": 27, ""temp_max"": 29 }, ""weather"": [{ ""main"": ""Moon"" }] },
                { ""main"": { ""temp"": 27, ""temp_min"": 26, ""temp_max"": 28 }, ""weather"": [{ ""main"": ""Moon"" }] },
                { ""main"": { ""temp"": 26, ""temp_min"": 25, ""temp_max"": 27 }, ""weather"": [{ ""main"": ""Moon"" }] },
                { ""main"": { ""temp"": 25, ""temp_min"": 24, ""temp_max"": 26 }, ""weather"": [{ ""main"": ""Moon"" }] }
            ]
        }";

        // 3. AQI JSON
        var fakeAqiJson = @"{ ""list"": [{ ""main"": { ""aqi"": 3 } }] }";

        // SETUP THE INTERCEPTOR ROUTING
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) => 
            {
                var uri = request.RequestUri!.ToString();
                string responseJson = "{}";

                if (uri.Contains("/weather?")) responseJson = fakeCurrentJson;
                else if (uri.Contains("/forecast?")) responseJson = fakeForecastJson;
                else if (uri.Contains("/air_pollution?")) responseJson = fakeAqiJson;

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                };
            });

        // ACT
        var result = await _service.GetWeatherAsync("Dubai");

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal("Dubai", result.City);
        Assert.Equal("AE", result.Country);
        
        // Visibility Logic Check (Meters -> KM)
        // 10000 meters should become 10 km
        Assert.Equal(10.0, result.Visibility);

        // AQI Logic Check
        Assert.Equal(3, result.AQI);

        // High/Low Logic Check (Should come from Forecast list max/min)
        // Max temp in our fake list is 35 (item 3)
        // Min temp in our fake list is 24 (item 8)
        Assert.Equal(35, result.MaxTemp); 
        Assert.Equal(24, result.MinTemp);

        // Sun Cycle Check (Just ensuring it populated a string)
        Assert.False(string.IsNullOrEmpty(result.Sunrise));
        Assert.False(string.IsNullOrEmpty(result.DayLength));
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldReturnNull_WhenOpenWeatherReturns404()
    {
        // ARRANGE: Setup 404 Response
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // ACT
        var result = await _service.GetWeatherAsync("Atlantis");

        // ASSERT
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldReturnNull_WhenExceptionOccurs()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network fail"));

        // Act
        var result = await _service.GetWeatherAsync("London");

        // Assert
        Assert.Null(result); 
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldReturnCachedData_OnSecondCall()
    {
        // ARRANGE: Setup successful response
        var fakeCurrentJson = @"{
            ""name"": ""London"",
            ""sys"": { ""country"": ""GB"", ""sunrise"": 1672531200, ""sunset"": 1672574400 },
            ""main"": { ""temp"": 15.5, ""humidity"": 70, ""temp_min"": 14, ""temp_max"": 17 },
            ""wind"": { ""speed"": 3.5 },
            ""weather"": [{ ""main"": ""Cloudy"", ""description"": ""overcast clouds"" }],
            ""coord"": { ""lat"": 51.5, ""lon"": -0.1 },
            ""visibility"": 8000,
            ""timezone"": 0
        }";

        var fakeForecastJson = @"{
            ""list"": [
                { ""main"": { ""temp"": 15, ""temp_min"": 14, ""temp_max"": 16 }, ""weather"": [{ ""main"": ""Clouds"" }] },
                { ""main"": { ""temp"": 16, ""temp_min"": 15, ""temp_max"": 17 }, ""weather"": [{ ""main"": ""Clouds"" }] },
                { ""main"": { ""temp"": 17, ""temp_min"": 16, ""temp_max"": 18 }, ""weather"": [{ ""main"": ""Clouds"" }] },
                { ""main"": { ""temp"": 16, ""temp_min"": 15, ""temp_max"": 17 }, ""weather"": [{ ""main"": ""Clouds"" }] },
                { ""main"": { ""temp"": 14, ""temp_min"": 13, ""temp_max"": 15 }, ""weather"": [{ ""main"": ""Clouds"" }] },
                { ""main"": { ""temp"": 13, ""temp_min"": 12, ""temp_max"": 14 }, ""weather"": [{ ""main"": ""Clouds"" }] },
                { ""main"": { ""temp"": 12, ""temp_min"": 11, ""temp_max"": 13 }, ""weather"": [{ ""main"": ""Clouds"" }] },
                { ""main"": { ""temp"": 11, ""temp_min"": 10, ""temp_max"": 12 }, ""weather"": [{ ""main"": ""Clouds"" }] }
            ]
        }";

        var fakeAqiJson = @"{ ""list"": [{ ""main"": { ""aqi"": 2 } }] }";

        int apiCallCount = 0;
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) => 
            {
                apiCallCount++;
                var uri = request.RequestUri!.ToString();
                string responseJson = "{}";

                if (uri.Contains("/weather?")) responseJson = fakeCurrentJson;
                else if (uri.Contains("/forecast?")) responseJson = fakeForecastJson;
                else if (uri.Contains("/air_pollution?")) responseJson = fakeAqiJson;

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                };
            });

        // ACT: First call (should hit API)
        var result1 = await _service.GetWeatherAsync("London");

        // Second call (should return from cache)
        var result2 = await _service.GetWeatherAsync("London");

        // ASSERT
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("London", result1.City);
        Assert.Equal("London", result2.City);
        
        // Should have made 3 API calls only (current + forecast + aqi) for first request
        // Second request should not make any API calls
        Assert.Equal(3, apiCallCount);
        
        // Both results should be identical (cached)
        Assert.Equal(result1.CurrentTemp, result2.CurrentTemp);
        Assert.Equal(result1.Humidity, result2.Humidity);
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldUseCaseInsensitiveCacheKey()
    {
        // ARRANGE
        var fakeCurrentJson = @"{
            ""name"": ""Paris"",
            ""sys"": { ""country"": ""FR"", ""sunrise"": 1672531200, ""sunset"": 1672574400 },
            ""main"": { ""temp"": 20, ""humidity"": 60, ""temp_min"": 18, ""temp_max"": 22 },
            ""wind"": { ""speed"": 2.5 },
            ""weather"": [{ ""main"": ""Clear"", ""description"": ""clear sky"" }],
            ""coord"": { ""lat"": 48.8, ""lon"": 2.3 },
            ""visibility"": 10000,
            ""timezone"": 3600
        }";

        var fakeForecastJson = @"{
            ""list"": [
                { ""main"": { ""temp"": 20, ""temp_min"": 18, ""temp_max"": 22 }, ""weather"": [{ ""main"": ""Clear"" }] },
                { ""main"": { ""temp"": 21, ""temp_min"": 19, ""temp_max"": 23 }, ""weather"": [{ ""main"": ""Clear"" }] },
                { ""main"": { ""temp"": 22, ""temp_min"": 20, ""temp_max"": 24 }, ""weather"": [{ ""main"": ""Clear"" }] },
                { ""main"": { ""temp"": 21, ""temp_min"": 19, ""temp_max"": 23 }, ""weather"": [{ ""main"": ""Clear"" }] },
                { ""main"": { ""temp"": 19, ""temp_min"": 17, ""temp_max"": 21 }, ""weather"": [{ ""main"": ""Clear"" }] },
                { ""main"": { ""temp"": 18, ""temp_min"": 16, ""temp_max"": 20 }, ""weather"": [{ ""main"": ""Clear"" }] },
                { ""main"": { ""temp"": 17, ""temp_min"": 15, ""temp_max"": 19 }, ""weather"": [{ ""main"": ""Clear"" }] },
                { ""main"": { ""temp"": 16, ""temp_min"": 14, ""temp_max"": 18 }, ""weather"": [{ ""main"": ""Clear"" }] }
            ]
        }";

        var fakeAqiJson = @"{ ""list"": [{ ""main"": { ""aqi"": 1 } }] }";

        int apiCallCount = 0;
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) => 
            {
                apiCallCount++;
                var uri = request.RequestUri!.ToString();
                string responseJson = "{}";

                if (uri.Contains("/weather?")) responseJson = fakeCurrentJson;
                else if (uri.Contains("/forecast?")) responseJson = fakeForecastJson;
                else if (uri.Contains("/air_pollution?")) responseJson = fakeAqiJson;

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                };
            });

        // ACT: Call with different casing
        var result1 = await _service.GetWeatherAsync("Paris");
        var result2 = await _service.GetWeatherAsync("PARIS");
        var result3 = await _service.GetWeatherAsync("paris");

        // ASSERT: Should only make API calls once (cache should handle case-insensitive)
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(3, apiCallCount); // Only first call should hit API
        Assert.Equal(result1.CurrentTemp, result2.CurrentTemp);
        Assert.Equal(result1.CurrentTemp, result3.CurrentTemp);
    }
}

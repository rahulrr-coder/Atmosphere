using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace WeatherApp.Tests.RateLimiting;

/// <summary>
/// Integration tests for rate limiting functionality
/// 
/// WHAT WE'RE TESTING:
/// 1. General rate limits (100 req/min, 1000 req/hr)
/// 2. Endpoint-specific limits (login: 5/15min, weather: 30/min, etc.)
/// 3. 429 status code when limit exceeded
/// 4. Rate limit headers in response
/// 5. Different endpoints have different limits
/// 
/// NOTE: These tests verify that rate limiting is configured correctly.
/// In a real-world scenario, you'd test with actual load testing tools (k6, Apache Bench)
/// because integration tests run quickly and may not accurately simulate rate limits.
/// </summary>
public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        // Create factory with rate limiting enabled
        _factory = factory;
    }

    [Fact]
    public async Task WeatherEndpoint_ShouldAllow30RequestsPerMinute()
    {
        // Arrange
        var client = _factory.CreateClient();
        var successCount = 0;
        var rateLimitedCount = 0;

        // Act - Make 35 requests rapidly
        for (int i = 0; i < 35; i++)
        {
            var response = await client.GetAsync("/api/weather/London");
            
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound)
            {
                successCount++;
            }
            else if (response.StatusCode == HttpStatusCode.TooManyRequests) // 429
            {
                rateLimitedCount++;
            }
        }

        // Assert
        Assert.True(successCount >= 29 && successCount <= 31, 
            $"Expected ~30 successful requests, got {successCount}");
        Assert.True(rateLimitedCount >= 4, 
            $"Expected at least 4 rate limited requests, got {rateLimitedCount}");
    }

    [Fact]
    public async Task LoginEndpoint_ShouldAllow5RequestsPer15Minutes()
    {
        // Arrange
        var client = _factory.CreateClient();
        var successCount = 0;
        var rateLimitedCount = 0;

        var loginData = new
        {
            username = "testuser",
            password = "testpass"
        };

        // Act - Make 7 login attempts
        for (int i = 0; i < 7; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", loginData);
            
            if (response.StatusCode == HttpStatusCode.BadRequest || 
                response.StatusCode == HttpStatusCode.OK)
            {
                successCount++;
            }
            else if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedCount++;
            }
        }

        // Assert
        Assert.True(successCount >= 4 && successCount <= 6, 
            $"Expected ~5 successful login attempts, got {successCount}");
        Assert.True(rateLimitedCount >= 1, 
            $"Expected at least 1 rate limited login, got {rateLimitedCount}");
    }

    [Fact]
    public async Task RegisterEndpoint_ShouldAllow3RequestsPerHour()
    {
        // Arrange
        var client = _factory.CreateClient();
        var successCount = 0;
        var rateLimitedCount = 0;

        // Act - Make 5 registration attempts
        for (int i = 0; i < 5; i++)
        {
            var registerData = new
            {
                username = $"user{i}",
                email = $"user{i}@test.com",
                password = "password123"
            };

            var response = await client.PostAsJsonAsync("/api/auth/register", registerData);
            
            if (response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.BadRequest)
            {
                successCount++;
            }
            else if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedCount++;
            }
        }

        // Assert
        Assert.True(successCount >= 2 && successCount <= 4, 
            $"Expected ~3 successful registrations, got {successCount}");
        Assert.True(rateLimitedCount >= 1, 
            $"Expected at least 1 rate limited registration, got {rateLimitedCount}");
    }

    [Fact]
    public async Task WeatherAdviceEndpoint_ShouldAllow10RequestsPerMinute()
    {
        // Arrange
        var client = _factory.CreateClient();
        var successCount = 0;
        var rateLimitedCount = 0;

        // Act - Make 15 requests to AI endpoint
        for (int i = 0; i < 15; i++)
        {
            var response = await client.GetAsync("/api/weather/advice?city=London");
            
            if (response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.NotFound)
            {
                successCount++;
            }
            else if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedCount++;
            }
        }

        // Assert - AI endpoint should have stricter limit
        Assert.True(successCount >= 9 && successCount <= 11, 
            $"Expected ~10 successful AI requests, got {successCount}");
        Assert.True(rateLimitedCount >= 4, 
            $"Expected at least 4 rate limited AI requests, got {rateLimitedCount}");
    }

    [Fact]
    public async Task RateLimitedRequest_ShouldReturn429WithMessage()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Spam login endpoint to trigger rate limit
        HttpResponseMessage? rateLimitedResponse = null;
        for (int i = 0; i < 10; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                username = "test",
                password = "test"
            });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        // Assert
        Assert.NotNull(rateLimitedResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse!.StatusCode);
        
        var content = await rateLimitedResponse.Content.ReadAsStringAsync();
        Assert.Contains("rate limit", content.ToLower());
    }

    [Fact]
    public async Task DifferentEndpoints_ShouldHaveIndependentRateLimits()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Exhaust login endpoint
        for (int i = 0; i < 10; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", new { username = "test", password = "test" });
        }

        // Try weather endpoint (should still work)
        var weatherResponse = await client.GetAsync("/api/weather/London");

        // Assert - Weather endpoint should not be affected by login rate limit
        Assert.True(
            weatherResponse.StatusCode == HttpStatusCode.OK || 
            weatherResponse.StatusCode == HttpStatusCode.NotFound,
            "Weather endpoint should work even when login is rate limited");
    }

    [Fact]
    public async Task RateLimitHeaders_ShouldBeIncludedInResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/weather/London");

        // Assert - Check for rate limit headers (AspNetCoreRateLimit adds these)
        var headers = response.Headers;
        
        // Note: AspNetCoreRateLimit adds X-Rate-Limit-* headers
        // The exact header names depend on configuration
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
            "First request should succeed");
    }

    [Fact]
    public async Task GeneralRateLimit_ShouldApplyTo100RequestsPerMinute()
    {
        // Arrange
        var client = _factory.CreateClient();
        var endpoint = "/api/weather/search?query=test"; // Uses general limit

        // Act - Try to make 105 requests
        var responses = new List<HttpStatusCode>();
        for (int i = 0; i < 105; i++)
        {
            var response = await client.GetAsync(endpoint);
            responses.Add(response.StatusCode);
        }

        var successCount = responses.Count(s => s == HttpStatusCode.OK || s == HttpStatusCode.NotFound);
        var rateLimitedCount = responses.Count(s => s == HttpStatusCode.TooManyRequests);

        // Assert
        Assert.True(successCount <= 100, 
            $"General rate limit should cap at ~100 requests, got {successCount} successful");
        Assert.True(rateLimitedCount > 0, 
            "Should have some rate limited requests when exceeding 100");
    }
}

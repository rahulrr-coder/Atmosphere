using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Services;
using WeatherApp.Services.Background;
using WeatherApp.Data;
using WeatherApp.Models;
using Quartz;

namespace WeatherApp.Tests.Background;

public class DailyWeatherJobTests
{
    private readonly Mock<IWeatherService> _mockWeather;
    private readonly Mock<IEmailService> _mockEmail;
    private readonly Mock<IAIService> _mockAi;
    private readonly WeatherDbContext _context; // We use In-Memory DB here
    private readonly DailyWeatherJob _job;

    public DailyWeatherJobTests()
    {
        _mockWeather = new Mock<IWeatherService>();
        _mockEmail = new Mock<IEmailService>();
        _mockAi = new Mock<IAIService>();

        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WeatherDbContext(options);

        _job = new DailyWeatherJob(_mockWeather.Object, _mockEmail.Object, _mockAi.Object, _context);
    }

    [Fact]
    public async Task Execute_ShouldSendEmails_ToSubscribedUsers()
    {
        // Arrange
        var user = new User { Username = "Test", Email = "t@t.com", SubscriptionCity = "London", IsSubscribed = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var fakeWeather = new WeatherModel { City = "London", CurrentTemp = 10 };
        _mockWeather.Setup(w => w.GetWeatherAsync("London")).ReturnsAsync(fakeWeather);
        _mockAi.Setup(a => a.GetFashionAdviceAsync(fakeWeather)).ReturnsAsync("Wear a coat.");

        // Act
        await _job.Execute(null); // Context isn't used, so null is fine

        // Assert
        // Verify Email Service was called exactly ONCE with correct data
        _mockEmail.Verify(e => e.SendWeatherEmailAsync("t@t.com", fakeWeather, "Wear a coat."), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldSkip_UnsubscribedUsers()
    {
        // Arrange
        var user = new User { Username = "Test", Email = "t@t.com", SubscriptionCity = "London", IsSubscribed = false };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _job.Execute(null);

        // Assert
        // Verify Email Service was NEVER called
        _mockEmail.Verify(e => e.SendWeatherEmailAsync(It.IsAny<string>(), It.IsAny<WeatherModel>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldSkip_IfWeatherServiceFails()
    {
        // Arrange
        var user = new User { Username = "Test", Email = "t@t.com", SubscriptionCity = "UnknownCity", IsSubscribed = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Weather service returns NULL (simulating failure/not found)
        _mockWeather.Setup(w => w.GetWeatherAsync("UnknownCity")).ReturnsAsync((WeatherModel?)null);

        // Act
        await _job.Execute(null);

        // Assert
        _mockEmail.Verify(e => e.SendWeatherEmailAsync(It.IsAny<string>(), It.IsAny<WeatherModel>(), It.IsAny<string>()), Times.Never);
    }
}
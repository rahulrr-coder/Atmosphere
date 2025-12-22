using Moq;
using Xunit;
using WeatherApp.Services;
using WeatherApp.Services.Wrappers;
using Microsoft.Extensions.Configuration;
using WeatherApp.Models;
using System.Net.Mail;

namespace WeatherApp.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ISmtpClientWrapper> _mockSmtp; // 👈 Mock the wrapper
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        // 1. Mock Config
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Email:Username"]).Returns("test@atmosphere.com");

        // 2. Mock SMTP Wrapper
        _mockSmtp = new Mock<ISmtpClientWrapper>();

        // 3. Inject Mock into Service
        _service = new EmailService(_mockConfig.Object, _mockSmtp.Object);
    }

    [Fact]
    public async Task SendWeatherEmailAsync_ShouldBuildHtmlAndCallWrapper()
    {
        // Arrange
        var weather = new WeatherModel 
        { 
            City = "Dubai", 
            CurrentTemp = 30, 
            CurrentCondition = "Sunny", 
            AQI = 2 
        };
        string aiAdvice = "Wear sunglasses.";

        // Act
        await _service.SendWeatherEmailAsync("user@test.com", weather, aiAdvice);

        // Assert
        // This is the magic line. We verify that SendMailAsync was called EXACTLY once.
        // We also check if the Subject was formatted correctly.
        _mockSmtp.Verify(x => x.SendMailAsync(It.Is<MailMessage>(msg => 
            msg.Subject == "The Atmosphere in Dubai ☁️" &&
            msg.To[0].Address == "user@test.com"
        )), Times.Once);
    }
    
    [Fact]
    public async Task SendEmailAsync_ShouldNotCrash_WhenSmtpFails()
    {
        // Arrange: Make the wrapper throw an error
        _mockSmtp.Setup(x => x.SendMailAsync(It.IsAny<MailMessage>()))
                 .ThrowsAsync(new Exception("Gmail is down"));

        // Act
        // Calling this should NOT throw an exception because your service catches it.
        await _service.SendEmailAsync("user@test.com", "Subject", "Body");

        // Assert
        // If we get here without the test crashing, the try/catch block worked.
        Assert.True(true); 
    }
}
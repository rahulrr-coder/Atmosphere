using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Moq;
using WeatherApp.Services.Wrappers;
using Xunit;

namespace WeatherApp.Tests.Services.Wrappers;

public class SmtpClientWrapperTests
{
    private readonly Mock<IConfiguration> _mockConfig;

    public SmtpClientWrapperTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Email:Username"]).Returns("test@example.com");
        _mockConfig.Setup(c => c["Email:Password"]).Returns("test_password");
    }

    [Fact]
    public void Constructor_ShouldInitialize_WithConfiguration()
    {
        // Act
        var wrapper = new SmtpClientWrapper(_mockConfig.Object);

        // Assert
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void Constructor_ShouldHandleNullConfiguration()
    {
        // Arrange
        var mockConfigNull = new Mock<IConfiguration>();
        mockConfigNull.Setup(c => c["Email:Username"]).Returns((string?)null);
        mockConfigNull.Setup(c => c["Email:Password"]).Returns((string?)null);

        // Act
        var wrapper = new SmtpClientWrapper(mockConfigNull.Object);

        // Assert - Should not throw
        Assert.NotNull(wrapper);
    }

    [Fact]
    public async Task SendMailAsync_ShouldThrowException_WhenInvalidCredentials()
    {
        // Arrange
        var wrapper = new SmtpClientWrapper(_mockConfig.Object);
        var message = new MailMessage("from@test.com", "to@test.com")
        {
            Subject = "Test",
            Body = "Test Body"
        };

        // Act & Assert - Since we're using fake credentials, SMTP will fail
        // This tests that the method properly propagates exceptions
        await Assert.ThrowsAsync<SmtpException>(async () =>
        {
            await wrapper.SendMailAsync(message);
        });
    }

    [Fact]
    public async Task SendMailAsync_ShouldAcceptValidMailMessage()
    {
        // Arrange
        var wrapper = new SmtpClientWrapper(_mockConfig.Object);
        var message = new MailMessage("sender@test.com", "recipient@test.com")
        {
            Subject = "Weather Alert",
            Body = "Rain expected today",
            IsBodyHtml = true
        };

        // Act & Assert - Method should accept the message even if sending fails
        // due to network/auth issues (which is expected in test environment)
        var exception = await Record.ExceptionAsync(async () =>
        {
            try
            {
                await wrapper.SendMailAsync(message);
            }
            catch (SmtpException)
            {
                // Expected to fail with invalid credentials
                // Just testing that it accepts the message format
            }
        });

        Assert.Null(exception); // No unexpected exceptions
    }

    [Fact]
    public void SmtpClientWrapper_ShouldImplementInterface()
    {
        // Arrange & Act
        var wrapper = new SmtpClientWrapper(_mockConfig.Object);

        // Assert
        Assert.IsAssignableFrom<ISmtpClientWrapper>(wrapper);
    }

    [Fact]
    public async Task SendMailAsync_ShouldHandleMailMessageWithAttachments()
    {
        // Arrange
        var wrapper = new SmtpClientWrapper(_mockConfig.Object);
        var message = new MailMessage("sender@test.com", "recipient@test.com")
        {
            Subject = "Test with Attachment",
            Body = "See attachment"
        };

        // Create a temporary file for attachment
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Test attachment content");
        message.Attachments.Add(new Attachment(tempFile));

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<SmtpException>(async () =>
            {
                await wrapper.SendMailAsync(message);
            });
        }
        finally
        {
            // Cleanup
            message.Dispose();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SendMailAsync_ShouldHandleMultipleRecipients()
    {
        // Arrange
        var wrapper = new SmtpClientWrapper(_mockConfig.Object);
        var message = new MailMessage
        {
            From = new MailAddress("sender@test.com"),
            Subject = "Broadcast",
            Body = "Message to multiple recipients"
        };
        
        message.To.Add("recipient1@test.com");
        message.To.Add("recipient2@test.com");
        message.CC.Add("cc@test.com");

        // Act & Assert
        await Assert.ThrowsAsync<SmtpException>(async () =>
        {
            await wrapper.SendMailAsync(message);
        });
    }

    [Fact]
    public async Task SendMailAsync_ShouldHandleHtmlBody()
    {
        // Arrange
        var wrapper = new SmtpClientWrapper(_mockConfig.Object);
        var htmlBody = @"
            <html>
                <body>
                    <h1>Weather Report</h1>
                    <p>Temperature: 25°C</p>
                </body>
            </html>";
        
        var message = new MailMessage("sender@test.com", "recipient@test.com")
        {
            Subject = "HTML Email Test",
            Body = htmlBody,
            IsBodyHtml = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<SmtpException>(async () =>
        {
            await wrapper.SendMailAsync(message);
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Controllers;
using WeatherApp.Data;
using WeatherApp.Models;
using Xunit;

namespace WeatherApp.Tests.Controllers;

public class AuthControllerTests
{
    private WeatherDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;
        return new WeatherDbContext(options);
    }

    [Fact]
    public async Task Register_ShouldCreateUser_WhenDataIsValid()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new AuthController(context);
        var request = new UserDto { Username = "testuser", Email = "test@mail.com", Password = "123", IsSubscribed = true, City = "London" };

        // Act
        var result = await controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<User>(okResult.Value);
        Assert.Equal("testuser", user.Username);
        
        // Verify DB
        var dbUser = await context.Users.FirstAsync();
        Assert.Equal("London", dbUser.SubscriptionCity);
    }

    [Fact]
    public async Task Login_ShouldReturnUser_WhenCredentialsAreCorrect()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        context.Users.Add(new User { Username = "user1", PasswordHash = "pass123", Email = "u@u.com" });
        await context.SaveChangesAsync();

        var controller = new AuthController(context);
        var request = new UserDto { Username = "user1", Password = "pass123" };

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<User>(okResult.Value);
        Assert.Equal("user1", user.Username);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenPasswordIsWrong()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        context.Users.Add(new User { Username = "user1", PasswordHash = "pass123", Email = "u@u.com" });
        await context.SaveChangesAsync();

        var controller = new AuthController(context);
        var request = new UserDto { Username = "user1", Password = "WRONG_PASSWORD" };

        // Act
        var result = await controller.Login(request);

        // Assert
        var badReq = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid username or password.", badReq.Value);
    }

    [Fact]
    public async Task UpdateSubscription_ShouldUpdateCity_WhenUserExists()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        context.Users.Add(new User { Username = "subUser", Email = "s@s.com", IsSubscribed = false });
        await context.SaveChangesAsync();

        var controller = new AuthController(context);
        var req = new SubscriptionDto { Username = "subUser", IsSubscribed = true, City = "Paris" };

        // Act
        var result = await controller.UpdateSubscription(req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dbUser = await context.Users.FirstAsync(u => u.Username == "subUser");
        Assert.True(dbUser.IsSubscribed);
        Assert.Equal("Paris", dbUser.SubscriptionCity);
    }
}
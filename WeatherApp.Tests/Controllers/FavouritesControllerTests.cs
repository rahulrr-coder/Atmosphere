using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Controllers;
using WeatherApp.Data;
using WeatherApp.Models;
using Xunit;

namespace WeatherApp.Tests.Controllers;

public class FavoritesControllerTests
{
    private WeatherDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WeatherDbContext(options);
    }

    [Fact]
    public async Task AddFavorite_ShouldAddCity_WhenUserExists()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        context.Users.Add(new User { Username = "favUser", Email = "f@f.com" });
        await context.SaveChangesAsync();

        var controller = new FavoritesController(context);
        var req = new FavRequest { Username = "favUser", CityName = "Tokyo" };

        // Act
        var result = await controller.AddFavorite(req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var cities = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        Assert.Contains("Tokyo", cities);

        // Verify DB
        var dbUser = await context.Users.Include(u => u.FavoriteCities).FirstAsync();
        Assert.Single(dbUser.FavoriteCities);
        Assert.Equal("Tokyo", dbUser.FavoriteCities[0].Name);
    }

    [Fact]
    public async Task AddFavorite_ShouldNotDuplicate_WhenCityAlreadyExists()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = new User { Username = "favUser", Email = "f@f.com" };
        user.FavoriteCities.Add(new FavoriteCity { Name = "Tokyo" });
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new FavoritesController(context);
        var req = new FavRequest { Username = "favUser", CityName = "Tokyo" }; // Same city

        // Act
        await controller.AddFavorite(req);

        // Assert
        var dbUser = await context.Users.Include(u => u.FavoriteCities).FirstAsync();
        Assert.Single(dbUser.FavoriteCities); // Should still be 1
    }

    [Fact]
    public async Task RemoveFavorite_ShouldRemoveCity_WhenItExists()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = new User { Username = "delUser", Email = "d@d.com" };
        user.FavoriteCities.Add(new FavoriteCity { Name = "Berlin" });
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new FavoritesController(context);

        // Act
        var result = await controller.RemoveFavorite("delUser", "Berlin");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var cities = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        Assert.Empty(cities);
    }
}
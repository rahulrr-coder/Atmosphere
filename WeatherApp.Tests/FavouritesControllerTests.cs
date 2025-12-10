using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Controllers;
using WeatherApp.Data;
using WeatherApp.Models;
using Xunit; // The testing framework

namespace WeatherApp.Tests;

public class FavoritesControllerTests
{
    // Helper Method: Creates a unique, empty "RAM Database" for every test
    private WeatherDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WeatherDbContext(options);
    }

    // TEST 1: Saving a City
    [Fact] // [Fact] means "This is a test" (Like 'it' in Vitest)
    public async Task Add_ShouldSaveCity_WhenCityIsNew()
    {
        // 1. Arrange (Setup)
        var db = GetInMemoryDb();
        var controller = new FavoritesController(db);
        var newCity = new FavoriteCity { Name = "Tokyo" };

        // 2. Act (The Logic)
        var result = await controller.Add(newCity);

        // 3. Assert (The Verification)
        // Did the API say "OK"?
        Assert.IsType<OkObjectResult>(result);

        // Did it actually save to the DB?
        var cityInDb = await db.Favorites.FirstOrDefaultAsync(c => c.Name == "Tokyo");
        Assert.NotNull(cityInDb); // Check if it exists
        Assert.Equal("Tokyo", cityInDb.Name); // Check if name matches
    }

    // TEST 2: Preventing Duplicates
    [Fact]
    public async Task Add_ShouldFail_WhenCityAlreadyExists()
    {
        // 1. Arrange
        var db = GetInMemoryDb();
        // Pre-fill the DB with "Paris"
        db.Favorites.Add(new FavoriteCity { Name = "Paris" });
        await db.SaveChangesAsync();

        var controller = new FavoritesController(db);
        var duplicateCity = new FavoriteCity { Name = "Paris" };

        // 2. Act
        var result = await controller.Add(duplicateCity);

        // 3. Assert
        // Should return "409 Conflict", not "200 OK"
        Assert.IsType<ConflictObjectResult>(result);
        
        // Count should still be 1, not 2
        Assert.Equal(1, await db.Favorites.CountAsync());
    }
}
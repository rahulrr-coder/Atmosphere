using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FavoritesController : ControllerBase
{
    private readonly WeatherDbContext _context;

    public FavoritesController(WeatherDbContext context)
    {
        _context = context;
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetFavorites(string username)
    {
        var user = await _context.Users.Include(u => u.FavoriteCities)
                                 .FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();
        return Ok(user.FavoriteCities.Select(f => f.Name)); // Just return names
    }

    [HttpPost]
    public async Task<IActionResult> AddFavorite([FromBody] FavRequest req)
    {
        var user = await _context.Users.Include(u => u.FavoriteCities)
                                 .FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null) return NotFound("User not found");

        if (!user.FavoriteCities.Any(f => f.Name.ToLower() == req.CityName.ToLower()))
        {
            user.FavoriteCities.Add(new FavoriteCity { Name = req.CityName });
            await _context.SaveChangesAsync();
        }
        return Ok(user.FavoriteCities.Select(f => f.Name));
    }

    [HttpDelete("{username}/{cityName}")]
    public async Task<IActionResult> RemoveFavorite(string username, string cityName)
    {
        var user = await _context.Users.Include(u => u.FavoriteCities)
                                 .FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        var city = user.FavoriteCities.FirstOrDefault(f => f.Name.ToLower() == cityName.ToLower());
        if (city != null)
        {
            user.FavoriteCities.Remove(city);
            await _context.SaveChangesAsync();
        }
        return Ok(user.FavoriteCities.Select(f => f.Name));
    }
}

public class FavRequest { public string Username { get; set; } = ""; public string CityName { get; set; } = ""; }
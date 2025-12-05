using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Controllers;

[ApiController]
[Route("[controller]")]
public class FavoritesController : ControllerBase
{
    private readonly WeatherDbContext _context;

    public FavoritesController(WeatherDbContext context)
    {
        _context = context;
    }

    // GET: /Favorites
    // Returns the list of all saved cities
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cities = await _context.Favorites.ToListAsync();
        return Ok(cities);
    }

    // POST: /Favorites
    // Saves a new city
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] FavoriteCity city)
    {
        // Check if it already exists to prevent duplicates
        var exists = await _context.Favorites.AnyAsync(c => c.Name == city.Name);
        if (exists) return Conflict("City already saved");

        _context.Favorites.Add(city);
        await _context.SaveChangesAsync(); // Writes to the DB
        
        return Ok(city);
    }

    // DELETE: /Favorites/5
    // Removes a city by ID
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var city = await _context.Favorites.FindAsync(id);
        if (city == null) return NotFound();
        
        _context.Favorites.Remove(city);
        await _context.SaveChangesAsync();
        
        return Ok();
    }
}
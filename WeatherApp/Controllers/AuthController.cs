using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly WeatherDbContext _context;

    public AuthController(WeatherDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(UserDto request)
    {
        // Simple register logic (hash password in real app)
        var user = new User 
        { 
            Username = request.Username, 
            Email = request.Email, 
            PasswordHash = request.Password,
            IsSubscribed = request.IsSubscribed,
            SubscriptionCity = request.City // Set initial city if provided
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<User>> Login(UserDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || user.PasswordHash != request.Password)
        {
            return BadRequest("Invalid username or password.");
        }
        return Ok(user);
    }

    
    [HttpPut("update-subscription")]
    public async Task<IActionResult> UpdateSubscription([FromBody] SubscriptionDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null) return NotFound("User not found");

        user.IsSubscribed = request.IsSubscribed;
        
        
        if (!string.IsNullOrEmpty(request.City))
        {
            user.SubscriptionCity = request.City;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Subscription updated", city = user.SubscriptionCity, status = user.IsSubscribed });
    }
}

// DTOs
public class UserDto 
{ 
    public string Username { get; set; } = ""; 
    public string Email { get; set; } = ""; 
    public string Password { get; set; } = ""; 
    public bool IsSubscribed { get; set; }
    public string City { get; set; } = "";
}

public class SubscriptionDto
{
    public string Username { get; set; } = "";
    public bool IsSubscribed { get; set; }
    public string City { get; set; } = "";
}
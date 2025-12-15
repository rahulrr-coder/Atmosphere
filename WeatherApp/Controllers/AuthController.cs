using BCrypt.Net; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly WeatherDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(WeatherDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // Updated DTO: Added IsSubscribed
    public class AuthRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsSubscribed { get; set; } = false;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        // 1. Check if username is taken
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest("Username already taken.");

        // 2. Hash the password
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Create the User (Updated to save Email and Subscription)
        var user = new User 
        { 
            Username = request.Username, 
            PasswordHash = passwordHash,
            Email = request.Email,              // Saving Email
            IsSubscribed = request.IsSubscribed // Saving Subscription Preference
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User registered successfully!", user.Username, user.IsSubscribed });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        // 1. Find user by Username
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        
        // 2. Security Check
        if (user == null) return BadRequest("User not found.");
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return BadRequest("Wrong password.");

        // 3. Create the Token
        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Get the key (Make sure "Jwt:Key" exists in your appsettings.json!)
        var keyString = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(keyString)) return StatusCode(500, "JWT Key is missing in config.");
        
        var key = Encoding.ASCII.GetBytes(keyString);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username) 
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return Ok(new { token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor)) });
    }

    [HttpPut("update-subscription")]
    public async Task<IActionResult> UpdateSubscription(string username, bool isSubscribed)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound("User not found");

        user.IsSubscribed = isSubscribed;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Subscription updated to: {isSubscribed}" });
    }
}
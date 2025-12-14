using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    // We store the HASH, not the password (e.g., "Argon2id...$#")
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // A user can have many favorite cities
    public List<FavoriteCity> FavoriteCities { get; set; } = new();
}
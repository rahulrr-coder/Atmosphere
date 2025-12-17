using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Email Subscription Settings
    public bool IsSubscribed { get; set; } = false;
    public string? SubscriptionCity { get; set; } 

    public List<FavoriteCity> FavoriteCities { get; set; } = new();
}
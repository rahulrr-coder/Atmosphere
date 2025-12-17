namespace WeatherApp.Models;

public class FavoriteCity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Foreign Key to User
    public int UserId { get; set; }
    // JsonIgnore prevents "Cycles" when sending data back to frontend
    [System.Text.Json.Serialization.JsonIgnore] 
    public User User { get; set; } = null!;
}
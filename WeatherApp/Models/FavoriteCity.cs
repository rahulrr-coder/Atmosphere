using System.Text.Json.Serialization;

namespace WeatherApp.Models;

public class FavoriteCity
{
    public int Id { get; set; }      // Primary Key for the Database
    public string Name { get; set; } // The City Name (e.g., "London")
    public int UserId {get;set;}
    [JsonIgnore]
    public User ? User {get;set;}
}

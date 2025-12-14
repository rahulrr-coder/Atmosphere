using Microsoft.EntityFrameworkCore;
using WeatherApp.Models;
namespace WeatherApp.Data;

public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }
    public DbSet<FavoriteCity> Favorites { get; set; }
    // 1. Add the Users Table
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 2. Define the Relationship (One-to-Many)
        modelBuilder.Entity<FavoriteCity>()
            .HasOne(f => f.User)           // A city has one User
            .WithMany(u => u.FavoriteCities) // A User has many cities
            .HasForeignKey(f => f.UserId)  // The link is "UserId"
            .OnDelete(DeleteBehavior.Cascade); // If User is deleted, delete their cities
    }
}
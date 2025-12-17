using Quartz;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Models;

namespace WeatherApp.Services.Background;

public class DailyWeatherJob : IJob
{
    private readonly IWeatherService _weatherService;
    private readonly IEmailService _emailService;
    private readonly IAIService _aiService;
    private readonly WeatherApp.Data.WeatherDbContext _context;

    public DailyWeatherJob(
        IWeatherService weatherService, 
        IEmailService emailService, 
        IAIService aiService, 
        WeatherApp.Data.WeatherDbContext context)
    {
        _weatherService = weatherService;
        _emailService = emailService;
        _aiService = aiService;
        _context = context;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        // 1. Get users who want emails AND have a valid city set
        var users = _context.Users
            .Where(u => u.IsSubscribed && !string.IsNullOrEmpty(u.Email) && !string.IsNullOrEmpty(u.SubscriptionCity))
            .ToList();

        Console.WriteLine($"📧 Daily Job Starting: Found {users.Count} subscribers to process.");

        foreach (var user in users)
        {
            // Strict check: if somehow city is null/empty here, skip them. No "Coimbatore" fallback.
            if (string.IsNullOrEmpty(user.SubscriptionCity)) continue;

            try 
            {
                // 2. Fetch Weather for THEIR saved city
                var weather = await _weatherService.GetWeatherAsync(user.SubscriptionCity);

                if (weather != null)
                {
                    // 3. Get AI Advice
                    var advice = await _aiService.GetFashionAdviceAsync(weather);

                    // 4. Send Email
                    await _emailService.SendWeatherEmailAsync(user.Email, weather, advice);
                    Console.WriteLine($"✅ Sent to {user.Username} for {user.SubscriptionCity}");
                }
                else
                {
                    Console.WriteLine($"⚠️ Could not fetch weather for {user.SubscriptionCity}. Skipping user {user.Username}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing user {user.Username}: {ex.Message}");
            }
        }
    }
}
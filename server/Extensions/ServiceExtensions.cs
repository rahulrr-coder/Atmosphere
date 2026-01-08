using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using WeatherApp.Data;
using WeatherApp.Services;
using WeatherApp.Services.AI;
using WeatherApp.Services.Background;
using WeatherApp.Services.Wrappers;

namespace WeatherApp.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WeatherDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<ISmtpClientWrapper, SmtpClientWrapper>();
        services.AddSingleton<IPromptService, PromptService>();
        
        return services;
    }

    public static IServiceCollection AddAIProviders(this IServiceCollection services)
    {
        // Register HttpClient for AI providers that need it
        services.AddHttpClient<CerebrasProvider>();
        services.AddHttpClient<GroqProvider>();
        services.AddHttpClient<GeminiProvider>();
        services.AddHttpClient<MistralProvider>();
        services.AddHttpClient<DeepSeekProvider>();

        // Register all AI providers (order matters - first to last fallback)
        services.AddTransient<IAIProvider, CerebrasProvider>();
        services.AddTransient<IAIProvider, GroqProvider>();
        services.AddTransient<IAIProvider, DeepSeekProvider>();
        services.AddTransient<IAIProvider, MistralProvider>();
        services.AddTransient<IAIProvider, GeminiProvider>();

        // Register the AI Service Manager (uses the providers above)
        services.AddTransient<IAIService, AIService>();
        
        return services;
    }

    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("DailyWeatherJob");
            q.AddJob<DailyWeatherJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DailyWeatherJob-trigger")
                // Runs at 8:00 AM daily
                .WithCronSchedule("0 0 8 ? * *")
                // Testing: every 30 seconds
                // .WithCronSchedule("0/30 * * ? * *")
            );
        });
        
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowVueApp", policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());
        });
        
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "super_secret_key_for_weather_app_maersk_demo_12345";
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
        
        return services;
    }
}

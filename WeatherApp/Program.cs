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

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. REGISTER SERVICES (Dependency Injection)
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Core Services
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<ISmtpClientWrapper, SmtpClientWrapper>();

// Register AI Service with HttpClient
builder.Services.AddHttpClient<IAIService, AIService>();

// 1. Cerebras (Fastest, High Quota)
builder.Services.AddTransient<IAIProvider, CerebrasProvider>();
// 2. Groq (Fast Fallback)
builder.Services.AddTransient<IAIProvider, GroqProvider>();
// 3. Gemini (Deep Fallback)
builder.Services.AddTransient<IAIProvider, GeminiProvider>();

// Register the Manager
builder.Services.AddTransient<IAIService, AIService>();

// Quartz (Background Jobs)
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("DailyWeatherJob");
    q.AddJob<DailyWeatherJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("DailyWeatherJob-trigger")
        // Runs at 8:00 AM daily
        .WithCronSchedule("0 0 8 ? * *") 
        // Testing
       //.WithCronSchedule("0/30 * * ? * *") 

    );
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);



// CORS: Defines WHO can access your API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy => 
        policy.AllowAnyOrigin()  // Allows requests from anywhere (localhost:3000, etc.)
              .AllowAnyMethod()  // Allows GET, POST, PUT, DELETE
              .AllowAnyHeader()); // Allows Custom Headers (like Authorization)
});

// Authentication (JWT)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_for_weather_app_maersk_demo_12345";
builder.Services.AddAuthentication(options =>
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
        ValidateIssuer = false,   // Simplified for Dev
        ValidateAudience = false  // Simplified for Dev
    };
});

var app = builder.Build();

// ==========================================
// 2. CONFIGURE PIPELINE (Middleware Order Matters!)
// ==========================================

// A. Database Migration on Startup (Optional but handy)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    db.Database.Migrate();
}

// B. Swagger (Documentation) - Should be early so we can see it
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// C. CORS - Must be BEFORE Auth
// Why? Browsers send a pre-flight check (OPTIONS) before the real request.
// If CORS is after Auth, the check fails because it has no token.
app.UseCors("AllowVueApp");

// D. Security - Auth (Who are you?) -> Authorization (Are you allowed?)
app.UseAuthentication();
app.UseAuthorization();

// E. Map the Endpoints
app.MapControllers();

app.Run();
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Extensions;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. REGISTER SERVICES (Dependency Injection)
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Custom service registrations (organized in ServiceExtensions)
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddCachingServices(); 
builder.Services.AddCoreServices();
builder.Services.AddAIProviders();
builder.Services.AddBackgroundJobs();
builder.Services.AddCorsPolicy();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);

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

// C. RATE LIMITING (BEFORE CORS & AUTH!)
app.UseIpRateLimiting();

// D. CORS - Must be BEFORE Auth
app.UseCors("AllowVueApp");

// E. Security 
app.UseAuthentication();
app.UseAuthorization();

// F. Map the Endpoints
app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
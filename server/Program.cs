using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Extensions;

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
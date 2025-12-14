using Microsoft.EntityFrameworkCore; 
using WeatherApp.Services;
using WeatherApp.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IWeatherService, WeatherApp.Services.WeatherService>();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cors service
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", //what's this policy and why it works this way?
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

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
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherApp.Data.WeatherDbContext>();
    db.Database.Migrate();
}

// get more clarity on the order of app.use
app.UseHttpsRedirection();
app.UseCors("AllowVueApp");
// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
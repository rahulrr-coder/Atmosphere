using Microsoft.EntityFrameworkCore; 
using WeatherApp.Services;
using WeatherApp.Data;
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
// learn lamda expressions in C# and using them
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", //what's this policy and why it works this way?
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// get more clarity on the order of app.use
app.UseHttpsRedirection();
app.UseCors("AllowVueApp");
// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();


app.UseAuthorization();

app.MapControllers();

app.Run();
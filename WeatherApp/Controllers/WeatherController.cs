using Microsoft.AspNetCore.Mvc;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    
    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet("{cityName}")]
    public async Task<IActionResult> Get(string cityName)
    {
        var data = await  _weatherService.GetWeatherAsync(cityName);

        if (data.Weather == "City Not Found")
        {
            return BadRequest(data);
        }
        
        return Ok(data);
    }
}


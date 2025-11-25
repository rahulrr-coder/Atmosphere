using Microsoft.AspNetCore.Mvc;

namespace WeatherApp.Controllers;
[ApiController]
[Route("[controller]")]

public class WeatherController
{
    [HttpGet]
    public string GetWeather()
    {
        return "It's sunny today";
    }
}
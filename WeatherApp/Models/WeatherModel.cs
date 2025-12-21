namespace WeatherApp.Models;

public class WeatherModel
{
    // Current Weather
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double CurrentTemp { get; set; }
    public string CurrentCondition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public int AQI { get; set; }

    // Daily High/Low
    public double MaxTemp { get; set; }
    public double MinTemp { get; set; }

    // New Data Points
    public double Visibility { get; set; } // in km
    public string Sunrise { get; set; } = ""; // e.g. "06:30 AM"
    public string Sunset { get; set; } = "";  // e.g. "06:15 PM"
    public string DayLength { get; set; } = ""; // e.g. "11h 45m"


    // The "Story" Segments
    public List<DayPartForecast> DayParts { get; set; } = new();
}

public class DayPartForecast
{
    public string PartName { get; set; } = "";
    public double Temp { get; set; }
    public string Condition { get; set; } = "";
}
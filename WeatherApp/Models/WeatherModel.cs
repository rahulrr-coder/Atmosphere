namespace WeatherApp.Models;

public class WeatherModel
{
    // Current Weather
    public string City { get; set; } = string.Empty;
    public string Country {get; set;} = string.Empty;
    public double CurrentTemp { get; set; }
    public string CurrentCondition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public int AQI { get; set; }

    // Daily High/Low
    public double MaxTemp { get; set; }
    public double MinTemp { get; set; }
    
    // The "Story" Segments (Morning, Afternoon, Evening)
    public List<DayPartForecast> DayParts { get; set; } = new();
}

public class DayPartForecast
{
    public string PartName { get; set; } = ""; // "Morning", "Afternoon", "Evening"
    public double Temp { get; set; }
    public string Condition { get; set; } = "";
}
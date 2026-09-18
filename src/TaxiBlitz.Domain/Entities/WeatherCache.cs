namespace TaxiBlitz.Domain.Entities
{
    public class WeatherCache
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public string ConditionText { get; set; }
        public string IconCode { get; set; }
        public double TempCelsius { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public DateTime ForecastDate { get; set; }
        public DateTime CachedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

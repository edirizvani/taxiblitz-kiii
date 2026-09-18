using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;

namespace TaxiBlitz.Infrastructure.Weather
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public WeatherService(HttpClient http, IConfiguration config, AppDbContext db)
        {
            _http   = http;
            _config = config;
            _db     = db;
        }

        public async Task<WeatherCache?> GetForecastAsync(string location, DateTime? date = null)
        {
            if (string.IsNullOrWhiteSpace(location)) return null;
            string apiKey = _config["OpenWeatherApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            var targetDate = date?.Date ?? DateTime.UtcNow.Date;

            // Return cached result if fresh (within 6 hours)
            var cached = await _db.WeatherCaches
                .Where(w => w.Location == location && w.ForecastDate == targetDate && w.CachedAtUtc > DateTime.UtcNow.AddHours(-6))
                .OrderByDescending(w => w.CachedAtUtc)
                .FirstOrDefaultAsync();
            if (cached != null) return cached;

            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/forecast?q={Uri.EscapeDataString(location)}&appid={apiKey}&units=metric&cnt=8";
                var response = await _http.GetFromJsonAsync<OpenWeatherResponse>(url);
                if (response?.list == null || !response.list.Any()) return null;

                var entry = response.list.FirstOrDefault() ?? response.list[0];
                var result = new WeatherCache
                {
                    Location      = location,
                    ConditionText = entry.weather?.FirstOrDefault()?.description ?? "Clear",
                    IconCode      = entry.weather?.FirstOrDefault()?.icon ?? "01d",
                    TempCelsius   = entry.main?.temp ?? 0,
                    TempMin       = entry.main?.temp_min ?? 0,
                    TempMax       = entry.main?.temp_max ?? 0,
                    ForecastDate  = targetDate,
                    CachedAtUtc   = DateTime.UtcNow
                };
                _db.WeatherCaches.Add(result);
                await _db.SaveChangesAsync();
                return result;
            }
            catch
            {
                return null;
            }
        }

        private class OpenWeatherResponse
        {
            public List<WeatherEntry> list { get; set; }
        }
        private class WeatherEntry
        {
            public WeatherMain main { get; set; }
            public List<WeatherCondition> weather { get; set; }
        }
        private class WeatherMain
        {
            public double temp { get; set; }
            public double temp_min { get; set; }
            public double temp_max { get; set; }
        }
        private class WeatherCondition
        {
            public string description { get; set; }
            public string icon { get; set; }
        }
    }
}

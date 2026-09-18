using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherCache?> GetForecastAsync(string location, DateTime? date = null);
    }
}

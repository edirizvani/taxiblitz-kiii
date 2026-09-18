using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;

namespace TaxiBlitz.Infrastructure.Maps
{
    public class LocationGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _db;

        public LocationGeocodingService(HttpClient httpClient, AppDbContext db)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TaxiBlitzOhrid", "1.0"));
            _db = db;
        }

        public async Task<GeoCoordinateCache> ResolveAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            var normalized = query.Trim().ToLowerInvariant();
            var cached = _db.GeoCoordinateCaches.FirstOrDefault(x => x.Query == normalized);
            if (cached != null)
                return cached;

            try
            {
                var requestUrl = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q=" + Uri.EscapeDataString(query);
                using var response = await _httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var tokens = JArray.Parse(payload);
                if (tokens.Count == 0)
                    return null;

                var first = tokens[0];
                var latitude  = ParseDouble(first["lat"]?.ToString());
                var longitude = ParseDouble(first["lon"]?.ToString());
                if (!latitude.HasValue || !longitude.HasValue)
                    return null;

                var cacheEntry = new GeoCoordinateCache
                {
                    Query       = normalized,
                    DisplayName = first["display_name"]?.ToString(),
                    Latitude    = latitude.Value,
                    Longitude   = longitude.Value,
                    Provider    = "OpenStreetMap Nominatim",
                    ResolvedAtUtc = DateTime.UtcNow
                };

                _db.GeoCoordinateCaches.Add(cacheEntry);
                await _db.SaveChangesAsync().ConfigureAwait(false);
                return cacheEntry;
            }
            catch
            {
                return null;
            }
        }

        private static double? ParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return null;
        }
    }
}

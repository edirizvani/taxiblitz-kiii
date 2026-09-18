using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Seo;

namespace TaxiBlitz.Infrastructure.Maps
{
    public class TourRouteService
    {
        private readonly LocationGeocodingService _geocodingService;

        public TourRouteService(LocationGeocodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        public async Task<IReadOnlyList<TourRoutePointDto>> BuildRoutePointsAsync(Tour tour)
        {
            if (tour == null)
                return Array.Empty<TourRoutePointDto>();

            var names = ExtractRouteNames(tour).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var points = new List<TourRoutePointDto>();

            for (var index = 0; index < names.Count; index++)
            {
                var name = names[index];
                var coordinates = await _geocodingService.ResolveAsync(name).ConfigureAwait(false);
                points.Add(new TourRoutePointDto
                {
                    Order       = index + 1,
                    Name        = name,
                    Description = index == 0 ? "Starting point" : index == names.Count - 1 ? "Ending point" : "Route stop",
                    Latitude    = coordinates?.Latitude,
                    Longitude   = coordinates?.Longitude
                });
            }

            return points;
        }

        public IReadOnlyList<string> ExtractRouteNames(Tour tour)
        {
            var routeNames = new List<string>();
            AddIfNotBlank(routeNames, tour.StartingPoint);

            foreach (var value in SplitRouteText(tour.RouteStopsText))
                AddIfNotBlank(routeNames, value);

            foreach (var value in SplitRouteText(tour.CulturalHighlights))
                AddIfNotBlank(routeNames, value);

            AddIfNotBlank(routeNames, tour.EndingPoint);
            return routeNames;
        }

        private static IEnumerable<string> SplitRouteText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var normalized = text.Replace("\r", "\n");
            foreach (var segment in Regex.Split(normalized, @"[\n;|•]+"))
            {
                var trimmed = segment.Trim();
                if (trimmed.Length == 0)
                    continue;

                foreach (var part in trimmed.Split(new[] { " - ", "->" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidate = part.Trim();
                    if (candidate.Length > 0)
                        yield return candidate;
                }
            }
        }

        private static void AddIfNotBlank(ICollection<string> items, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                items.Add(value.Trim());
        }
    }
}

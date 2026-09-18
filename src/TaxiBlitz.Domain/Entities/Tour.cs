using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TaxiBlitz.Domain.Entities
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tour Title")]
        [StringLength(100, ErrorMessage = "The title cannot exceed 100 characters.")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Description")]
        [StringLength(5000, ErrorMessage = "The description is too lengthy.")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Price")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Duration")]
        [StringLength(50, ErrorMessage = "Duration format is incorrect.")]
        public string Duration { get; set; }

        [Display(Name = "SEO Slug")]
        [StringLength(200)]
        public string? Slug { get; set; }

        [Display(Name = "YouTube Video Link")]
        [DataType(DataType.Url)]
        public string YouTubeLink { get; set; }

        [Display(Name = "Profile Photo URL")]
        [DataType(DataType.ImageUrl)]
        public string PhotoProfileUrl { get; set; }

        [Display(Name = "Route Stops")]
        [StringLength(2000, ErrorMessage = "The route stops text is too lengthy.")]
        public string RouteStopsText { get; set; }

        // Additional fields to capture the location and cultural insights
        [NotMapped]
        [Display(Name = "Tour Stops")]
        public List<string> Stops
        {
            get { return ParseRouteStops(); }
            set { RouteStopsText = value == null ? null : string.Join("\n", value.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())); }
        }

        [Display(Name = "Cultural Highlights")]
        [StringLength(500, ErrorMessage = "The cultural highlights description is too lengthy.")]
        public string CulturalHighlights { get; set; }

        // Geographical Attributes
        [Display(Name = "Starting Point")]
        public string StartingPoint { get; set; }

        [Display(Name = "Ending Point")]
        public string EndingPoint { get; set; }

        [NotMapped]
        [Display(Name = "Is International")]
        public bool IsInternational => ParseRouteStops().Any(stop => stop.IndexOf("Albania", StringComparison.OrdinalIgnoreCase) >= 0);

        [NotMapped]
        public IReadOnlyList<string> RouteStops => ParseRouteStops();

        private List<string> ParseRouteStops()
        {
            var stops = new List<string>();

            if (!string.IsNullOrWhiteSpace(StartingPoint))
            {
                stops.Add(StartingPoint.Trim());
            }

            if (!string.IsNullOrWhiteSpace(RouteStopsText))
            {
                stops.AddRange(RouteStopsText
                    .Replace("\r", "\n")
                    .Split(new[] { '\n', ';', '|', '•' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
            }

            if (!string.IsNullOrWhiteSpace(CulturalHighlights))
            {
                stops.AddRange(CulturalHighlights
                    .Replace("\r", "\n")
                    .Split(new[] { '\n', ';', '|', '•' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
            }

            if (!string.IsNullOrWhiteSpace(EndingPoint))
            {
                stops.Add(EndingPoint.Trim());
            }

            return stops
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}

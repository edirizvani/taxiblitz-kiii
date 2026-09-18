using System;
using System.Collections.Generic;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Domain.Seo
{
    public sealed class BreadcrumbItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public bool IsCurrentPage { get; set; }
    }

    public sealed class FaqItem
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    public sealed class TourRoutePointDto
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string City { get; set; }
        public string ImageUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public sealed class TourSeoViewModel
    {
        public Tour Tour { get; set; }
        public string CanonicalUrl { get; set; }
        public string ImageUrl { get; set; }
        public IReadOnlyList<TourRoutePointDto> RoutePoints { get; set; } = Array.Empty<TourRoutePointDto>();
        public IReadOnlyList<FaqItem> FaqItems { get; set; } = Array.Empty<FaqItem>();
        public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; set; } = Array.Empty<BreadcrumbItem>();
        public double? AverageRating { get; set; }
        public int? ReviewCount { get; set; }
    }
}


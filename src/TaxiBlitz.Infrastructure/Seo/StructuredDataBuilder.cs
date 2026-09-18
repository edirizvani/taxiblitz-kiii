using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Seo;

namespace TaxiBlitz.Infrastructure.Seo
{
    public static class StructuredDataBuilder
    {
        public static IReadOnlyList<JObject> BuildGlobalSchemas(string siteName, string siteUrl, string description, string phone, string email)
        {
            var address = new JObject
            {
                ["@type"]           = "PostalAddress",
                ["streetAddress"]   = "Ohrid",
                ["addressLocality"] = "Ohrid",
                ["addressRegion"]   = "Southwestern Region",
                ["addressCountry"]  = "MK",
                ["postalCode"]      = "6000"
            };

            var geo = new JObject
            {
                ["@type"]     = "GeoCoordinates",
                ["latitude"]  = 41.1172,
                ["longitude"] = 20.8016
            };

            var schemas = new List<JObject>
            {
                CreateSchema("WebSite", new Dictionary<string, object>
                {
                    ["name"]        = siteName,
                    ["url"]         = siteUrl,
                    ["description"] = description,
                    ["potentialAction"] = new JObject
                    {
                        ["@type"]       = "SearchAction",
                        ["target"]      = siteUrl.TrimEnd('/') + "/Tours?q={search_term_string}",
                        ["query-input"] = "required name=search_term_string"
                    }
                }),
                CreateSchema("Organization", new Dictionary<string, object>
                {
                    ["name"]        = siteName,
                    ["url"]         = siteUrl,
                    ["description"] = description,
                    ["email"]       = email,
                    ["telephone"]   = phone,
                    ["address"]     = address,
                    ["logo"]        = new JObject
                    {
                        ["@type"] = "ImageObject",
                        ["url"]   = siteUrl.TrimEnd('/') + "/img/og-image.jpg"
                    },
                    ["sameAs"] = new JArray
                    {
                        "https://wa.me/38970589874"
                    }
                }),
                CreateSchema("TaxiService", new Dictionary<string, object>
                {
                    ["name"]        = siteName,
                    ["url"]         = siteUrl,
                    ["description"] = description,
                    ["email"]       = email,
                    ["telephone"]   = phone,
                    ["address"]     = address,
                    ["geo"]         = geo,
                    ["areaServed"]  = new JArray
                    {
                        new JObject { ["@type"] = "City", ["name"] = "Ohrid" },
                        new JObject { ["@type"] = "City", ["name"] = "Skopje" },
                        new JObject { ["@type"] = "City", ["name"] = "Tirana" },
                        new JObject { ["@type"] = "City", ["name"] = "Struga" },
                        new JObject { ["@type"] = "City", ["name"] = "Bitola" }
                    },
                    ["openingHoursSpecification"] = new JObject
                    {
                        ["@type"]     = "OpeningHoursSpecification",
                        ["dayOfWeek"] = new JArray { "Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday" },
                        ["opens"]     = "00:00",
                        ["closes"]    = "23:59"
                    },
                    ["priceRange"] = "€€",
                    ["currenciesAccepted"] = "EUR",
                    ["paymentAccepted"]    = "Cash, Bank Transfer",
                    ["hasMap"]             = "https://maps.google.com/?q=Ohrid,North+Macedonia"
                })
            };

            return schemas;
        }

        public static JObject BuildBreadcrumbSchema(IEnumerable<BreadcrumbItem> breadcrumbs)
        {
            var items = breadcrumbs?.Where(x => !string.IsNullOrWhiteSpace(x?.Name)).ToList() ?? new List<BreadcrumbItem>();
            if (items.Count == 0)
                return null;

            var listItems = new JArray();
            for (var i = 0; i < items.Count; i++)
            {
                var breadcrumb = items[i];
                var listItem = new JObject
                {
                    ["@type"] = "ListItem",
                    ["position"] = i + 1,
                    ["name"] = breadcrumb.Name
                };

                if (!string.IsNullOrWhiteSpace(breadcrumb.Url))
                    listItem["item"] = breadcrumb.Url;

                listItems.Add(listItem);
            }

            return CreateSchema("BreadcrumbList", new Dictionary<string, object>
            {
                ["itemListElement"] = listItems
            });
        }

        public static IReadOnlyList<JObject> BuildTourSchemas(TourSeoViewModel seo)
        {
            if (seo?.Tour == null)
                return Array.Empty<JObject>();

            var schemas = new List<JObject>();
            var tour = seo.Tour;
            var routePoints = seo.RoutePoints?.Where(x => !string.IsNullOrWhiteSpace(x?.Name)).ToList() ?? new List<TourRoutePointDto>();

            var itineraryItems = new JArray();
            foreach (var point in routePoints)
            {
                var item = new JObject
                {
                    ["@type"] = "ListItem",
                    ["position"] = point.Order,
                    ["name"] = point.Name
                };

                if (!string.IsNullOrWhiteSpace(point.Description))
                    item["description"] = point.Description;

                itineraryItems.Add(item);
            }

            var touristTrip = CreateSchema("TouristTrip", new Dictionary<string, object>
            {
                ["name"] = tour.Title,
                ["description"] = tour.Description,
                ["image"] = seo.ImageUrl,
                ["touristType"] = "Sightseeing Travelers",
                ["itinerary"] = new JObject
                {
                    ["@type"] = "ItemList",
                    ["itemListElement"] = itineraryItems
                },
                ["departureLocation"] = CreatePlace(tour.StartingPoint, routePoints.FirstOrDefault()),
                ["arrivalLocation"] = CreatePlace(tour.EndingPoint, routePoints.LastOrDefault())
            });

            schemas.Add(touristTrip);

            var product = CreateSchema("Product", new Dictionary<string, object>
            {
                ["name"] = tour.Title,
                ["description"] = tour.Description,
                ["image"] = seo.ImageUrl,
                ["brand"] = new JObject
                {
                    ["@type"] = "Brand",
                    ["name"] = "Taxi Blitz Ohrid"
                },
                ["offers"] = new JObject
                {
                    ["@type"] = "Offer",
                    ["priceCurrency"] = "EUR",
                    ["price"] = tour.Price.ToString(CultureInfo.InvariantCulture),
                    ["availability"] = "https://schema.org/InStock",
                    ["url"] = seo.CanonicalUrl
                }
            });

            if (seo.AverageRating.HasValue && seo.ReviewCount.GetValueOrDefault() > 0)
            {
                product["aggregateRating"] = new JObject
                {
                    ["@type"] = "AggregateRating",
                    ["ratingValue"] = Math.Round(seo.AverageRating.Value, 1),
                    ["reviewCount"] = seo.ReviewCount.Value,
                    ["bestRating"]  = 5,
                    ["worstRating"] = 1
                };
            }

            schemas.Add(product);

            if (seo.FaqItems != null && seo.FaqItems.Count > 0)
            {
                var faqEntities = new JArray();
                foreach (var faq in seo.FaqItems.Where(x => !string.IsNullOrWhiteSpace(x?.Question) && !string.IsNullOrWhiteSpace(x?.Answer)))
                {
                    faqEntities.Add(new JObject
                    {
                        ["@type"] = "Question",
                        ["name"] = faq.Question,
                        ["acceptedAnswer"] = new JObject
                        {
                            ["@type"] = "Answer",
                            ["text"] = faq.Answer
                        }
                    });
                }

                if (faqEntities.Count > 0)
                {
                    schemas.Add(CreateSchema("FAQPage", new Dictionary<string, object>
                    {
                        ["mainEntity"] = faqEntities
                    }));
                }
            }

            return schemas;
        }

        public static IReadOnlyList<JObject> BuildTourListSchemas(IEnumerable<Tour> tours)
        {
            var list = new List<JObject>();
            if (tours == null) return list;

            foreach (var tour in tours)
            {
                if (tour == null) continue;

                var product = CreateSchema("Product", new Dictionary<string, object>
                {
                    ["name"] = tour.Title,
                    ["description"] = string.IsNullOrWhiteSpace(tour.Description) ? null : tour.Description,
                    ["image"] = string.IsNullOrWhiteSpace(tour.PhotoProfileUrl) ? null : tour.PhotoProfileUrl,
                    ["brand"] = new JObject { ["@type"] = "Brand", ["name"] = "Taxi Blitz Ohrid" },
                    ["offers"] = new JObject
                    {
                        ["@type"] = "Offer",
                        ["priceCurrency"] = "EUR",
                        ["price"] = tour.Price.ToString(CultureInfo.InvariantCulture),
                        ["availability"] = "https://schema.org/InStock"
                    }
                });

                list.Add(product);
            }

            return list;
        }

        private static JObject CreateSchema(string type, IDictionary<string, object> values)
        {
            var schema = JObject.FromObject(values ?? new Dictionary<string, object>());
            schema["@context"] = "https://schema.org";
            schema["@type"] = type;
            return schema;
        }

        private static JObject CreatePlace(string fallbackName, TourRoutePointDto point)
        {
            if (point == null)
            {
                return string.IsNullOrWhiteSpace(fallbackName)
                    ? null
                    : new JObject { ["@type"] = "Place", ["name"] = fallbackName };
            }

            var place = new JObject { ["@type"] = "Place", ["name"] = point.Name };

            if (!string.IsNullOrWhiteSpace(point.Description))
                place["description"] = point.Description;

            if (point.Latitude.HasValue && point.Longitude.HasValue)
            {
                place["geo"] = new JObject
                {
                    ["@type"] = "GeoCoordinates",
                    ["latitude"] = point.Latitude.Value,
                    ["longitude"] = point.Longitude.Value
                };
            }

            return place;
        }
    }
}

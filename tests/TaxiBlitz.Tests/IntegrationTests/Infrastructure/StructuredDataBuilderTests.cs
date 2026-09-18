using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Seo;
using TaxiBlitz.Infrastructure.Seo;

namespace TaxiBlitz.Tests.IntegrationTests.Infrastructure;

public class StructuredDataBuilderTests
{
    // ── BuildGlobalSchemas ───────────────────────────────────────────

    [Fact]
    public void BuildGlobalSchemas_ReturnsThreeSchemas()
    {
        var schemas = StructuredDataBuilder.BuildGlobalSchemas("TaxiBlitz", "https://tb.mk", "Desc", "+389", "info@tb.mk");
        Assert.Equal(3, schemas.Count);
    }

    [Fact]
    public void BuildGlobalSchemas_FirstSchema_IsWebSite()
    {
        var schemas = StructuredDataBuilder.BuildGlobalSchemas("TaxiBlitz", "https://tb.mk", "Desc", "+389", "info@tb.mk");
        Assert.Equal("WebSite", schemas[0]["@type"]!.ToString());
    }

    [Fact]
    public void BuildGlobalSchemas_SecondSchema_IsOrganization()
    {
        var schemas = StructuredDataBuilder.BuildGlobalSchemas("TaxiBlitz", "https://tb.mk", "Desc", "+389", "info@tb.mk");
        Assert.Equal("Organization", schemas[1]["@type"]!.ToString());
    }

    [Fact]
    public void BuildGlobalSchemas_ThirdSchema_IsTaxiService()
    {
        var schemas = StructuredDataBuilder.BuildGlobalSchemas("TaxiBlitz", "https://tb.mk", "Desc", "+389", "info@tb.mk");
        Assert.Equal("TaxiService", schemas[2]["@type"]!.ToString());
    }

    [Fact]
    public void BuildGlobalSchemas_AllSchemas_HaveAtContext()
    {
        var schemas = StructuredDataBuilder.BuildGlobalSchemas("TaxiBlitz", "https://tb.mk", "Desc", "+389", "info@tb.mk");
        Assert.All(schemas, s => Assert.Equal("https://schema.org", s["@context"]!.ToString()));
    }

    // ── BuildBreadcrumbSchema ────────────────────────────────────────

    [Fact]
    public void BuildBreadcrumbSchema_WithItems_ReturnsBreadcrumbList()
    {
        var breadcrumbs = new[] { new BreadcrumbItem { Name = "Home", Url = "/" }, new BreadcrumbItem { Name = "Tours", Url = "/Tours" } };
        var result = StructuredDataBuilder.BuildBreadcrumbSchema(breadcrumbs);
        Assert.NotNull(result);
        Assert.Equal("BreadcrumbList", result!["@type"]!.ToString());
    }

    [Fact]
    public void BuildBreadcrumbSchema_SetsPositionOneIndexed()
    {
        var breadcrumbs = new[] { new BreadcrumbItem { Name = "Home", Url = "/" }, new BreadcrumbItem { Name = "Tours", Url = "/Tours" } };
        var result = StructuredDataBuilder.BuildBreadcrumbSchema(breadcrumbs);
        var items = result!["itemListElement"]!;
        Assert.Equal("1", items[0]!["position"]!.ToString());
        Assert.Equal("2", items[1]!["position"]!.ToString());
    }

    [Fact]
    public void BuildBreadcrumbSchema_WithEmptyList_ReturnsNull()
    {
        var result = StructuredDataBuilder.BuildBreadcrumbSchema(Array.Empty<BreadcrumbItem>());
        Assert.Null(result);
    }

    [Fact]
    public void BuildBreadcrumbSchema_FiltersNullOrEmptyNames()
    {
        var breadcrumbs = new[] { new BreadcrumbItem { Name = "", Url = "/" } };
        var result = StructuredDataBuilder.BuildBreadcrumbSchema(breadcrumbs);
        Assert.Null(result);
    }

    [Fact]
    public void BuildBreadcrumbSchema_IncludesUrlWhenProvided()
    {
        var breadcrumbs = new[] { new BreadcrumbItem { Name = "Home", Url = "https://tb.mk" } };
        var result = StructuredDataBuilder.BuildBreadcrumbSchema(breadcrumbs);
        var item = result!["itemListElement"]![0]!;
        Assert.NotNull(item["item"]);
    }

    [Fact]
    public void BuildBreadcrumbSchema_OmitsUrlWhenEmpty()
    {
        var breadcrumbs = new[] { new BreadcrumbItem { Name = "Home", Url = "" } };
        var result = StructuredDataBuilder.BuildBreadcrumbSchema(breadcrumbs);
        var item = result!["itemListElement"]![0]!;
        Assert.Null(item["item"]);
    }

    // ── BuildTourSchemas ─────────────────────────────────────────────

    [Fact]
    public void BuildTourSchemas_WithNullSeo_ReturnsEmpty()
    {
        var result = StructuredDataBuilder.BuildTourSchemas(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildTourSchemas_WithNullTour_ReturnsEmpty()
    {
        var result = StructuredDataBuilder.BuildTourSchemas(new TourSeoViewModel { Tour = null! });
        Assert.Empty(result);
    }

    [Fact]
    public void BuildTourSchemas_ReturnsTouristTripAndProduct()
    {
        var seo = new TourSeoViewModel
        {
            Tour = new Tour { Title = "Lake Tour", Description = "D", Price = 100, Duration = "4h" },
            CanonicalUrl = "https://tb.mk/tours/1",
            ImageUrl = "https://img.jpg"
        };

        var result = StructuredDataBuilder.BuildTourSchemas(seo);

        Assert.True(result.Count >= 2);
    }

    [Fact]
    public void BuildTourSchemas_TouristTrip_HasCorrectAtType()
    {
        var seo = new TourSeoViewModel
        {
            Tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" },
            CanonicalUrl = "https://tb.mk/1"
        };

        var result = StructuredDataBuilder.BuildTourSchemas(seo);

        Assert.Equal("TouristTrip", result[0]["@type"]!.ToString());
    }

    [Fact]
    public void BuildTourSchemas_Product_HasCorrectAtType()
    {
        var seo = new TourSeoViewModel
        {
            Tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" },
            CanonicalUrl = "https://tb.mk/1"
        };

        var result = StructuredDataBuilder.BuildTourSchemas(seo);

        Assert.Equal("Product", result[1]["@type"]!.ToString());
    }

    [Fact]
    public void BuildTourSchemas_WithRatingAndReviews_AddsAggregateRating()
    {
        var seo = new TourSeoViewModel
        {
            Tour         = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" },
            CanonicalUrl = "https://tb.mk/1",
            AverageRating = 4.5,
            ReviewCount   = 10
        };

        var result = StructuredDataBuilder.BuildTourSchemas(seo);
        var product = result.First(s => s["@type"]!.ToString() == "Product");

        Assert.NotNull(product["aggregateRating"]);
    }

    [Fact]
    public void BuildTourSchemas_WithNoReviews_OmitsAggregateRating()
    {
        var seo = new TourSeoViewModel
        {
            Tour         = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" },
            CanonicalUrl = "https://tb.mk/1",
            ReviewCount  = 0
        };

        var result = StructuredDataBuilder.BuildTourSchemas(seo);
        var product = result.First(s => s["@type"]!.ToString() == "Product");

        Assert.Null(product["aggregateRating"]);
    }

    [Fact]
    public void BuildTourSchemas_WithFaqItems_AddsFaqPage()
    {
        var seo = new TourSeoViewModel
        {
            Tour         = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" },
            CanonicalUrl = "https://tb.mk/1",
            FaqItems     = new[] { new FaqItem { Question = "Q?", Answer = "A." } }
        };

        var result = StructuredDataBuilder.BuildTourSchemas(seo);

        Assert.Contains(result, s => s["@type"]!.ToString() == "FAQPage");
    }

    [Fact]
    public void BuildTourSchemas_WithEmptyFaqItems_OmitsFaqPage()
    {
        var seo = new TourSeoViewModel
        {
            Tour         = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" },
            CanonicalUrl = "https://tb.mk/1",
            FaqItems     = Array.Empty<FaqItem>()
        };

        var result = StructuredDataBuilder.BuildTourSchemas(seo);

        Assert.DoesNotContain(result, s => s["@type"]!.ToString() == "FAQPage");
    }

    // ── BuildTourListSchemas ─────────────────────────────────────────

    [Fact]
    public void BuildTourListSchemas_WithTours_ReturnsProductPerTour()
    {
        var tours = new[]
        {
            new Tour { Title = "A", Description = "D", Price = 10, Duration = "1h" },
            new Tour { Title = "B", Description = "D", Price = 20, Duration = "2h" },
            new Tour { Title = "C", Description = "D", Price = 30, Duration = "3h" }
        };

        var result = StructuredDataBuilder.BuildTourListSchemas(tours);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void BuildTourListSchemas_WithNull_ReturnsEmpty()
    {
        var result = StructuredDataBuilder.BuildTourListSchemas(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildTourListSchemas_EachSchema_IsProduct()
    {
        var tours = new[]
        {
            new Tour { Title = "A", Description = "D", Price = 10, Duration = "1h" },
            new Tour { Title = "B", Description = "D", Price = 20, Duration = "2h" }
        };

        var result = StructuredDataBuilder.BuildTourListSchemas(tours);

        Assert.All(result, s => Assert.Equal("Product", s["@type"]!.ToString()));
    }

    [Fact]
    public void BuildTourListSchemas_Product_HasPriceCurrencyEUR()
    {
        var tours = new[] { new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" } };

        var result = StructuredDataBuilder.BuildTourListSchemas(tours);

        Assert.Equal("EUR", result[0]["offers"]!["priceCurrency"]!.ToString());
    }
}

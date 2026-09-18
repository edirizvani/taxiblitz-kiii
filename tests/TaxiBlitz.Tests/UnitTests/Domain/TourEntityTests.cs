using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Domain;

public class TourEntityTests
{
    // ── ParseRouteStops / RouteStops ─────────────────────────────────

    [Fact]
    public void ParseRouteStops_WithNewlineSeparators_ReturnsSplitStops()
    {
        var tour = new Tour { RouteStopsText = "Ohrid\nStruga\nPogradec" };
        Assert.Equal(3, tour.RouteStops.Count);
        Assert.Contains("Ohrid", tour.RouteStops);
        Assert.Contains("Struga", tour.RouteStops);
        Assert.Contains("Pogradec", tour.RouteStops);
    }

    [Fact]
    public void ParseRouteStops_WithSemicolonSeparators_ReturnsSplitStops()
    {
        var tour = new Tour { RouteStopsText = "Ohrid;Struga;Pogradec" };
        Assert.Equal(3, tour.RouteStops.Count);
    }

    [Fact]
    public void ParseRouteStops_WithPipeSeparators_ReturnsSplitStops()
    {
        var tour = new Tour { RouteStopsText = "Ohrid|Struga|Pogradec" };
        Assert.Equal(3, tour.RouteStops.Count);
    }

    [Fact]
    public void ParseRouteStops_WithBulletSeparators_ReturnsSplitStops()
    {
        var tour = new Tour { RouteStopsText = "Ohrid•Struga" };
        Assert.Equal(2, tour.RouteStops.Count);
    }

    [Fact]
    public void ParseRouteStops_IncludesStartingAndEndingPoint()
    {
        var tour = new Tour { StartingPoint = "Ohrid", EndingPoint = "Struga", RouteStopsText = null };
        Assert.Contains("Ohrid", tour.RouteStops);
        Assert.Contains("Struga", tour.RouteStops);
        Assert.Equal("Ohrid", tour.RouteStops[0]);
        Assert.Equal("Struga", tour.RouteStops[^1]);
    }

    [Fact]
    public void ParseRouteStops_DeduplicatesCaseInsensitive()
    {
        var tour = new Tour { StartingPoint = "ohrid", RouteStopsText = "Ohrid\nStruga" };
        // "ohrid" and "Ohrid" should deduplicate to one entry
        Assert.Equal(1, tour.RouteStops.Count(s => s.Equals("ohrid", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void ParseRouteStops_WithNullRouteStopsText_ReturnsStartEndOnly()
    {
        var tour = new Tour { StartingPoint = "A", EndingPoint = "B", RouteStopsText = null };
        Assert.Equal(2, tour.RouteStops.Count);
    }

    [Fact]
    public void ParseRouteStops_WithAllNullFields_ReturnsEmptyList()
    {
        var tour = new Tour();
        Assert.Empty(tour.RouteStops);
    }

    [Fact]
    public void ParseRouteStops_WithEmptyString_ReturnsEmptyList()
    {
        var tour = new Tour { RouteStopsText = "", StartingPoint = "", EndingPoint = "" };
        Assert.Empty(tour.RouteStops);
    }

    // ── IsInternational ──────────────────────────────────────────────

    [Fact]
    public void IsInternational_ReturnsFalse_WhenNoAlbaniaInRoute()
    {
        var tour = new Tour { RouteStopsText = "Ohrid\nStruga", EndingPoint = "Bitola" };
        Assert.False(tour.IsInternational);
    }

    [Fact]
    public void IsInternational_ReturnsTrue_WhenAlbaniaInRouteStopsText()
    {
        var tour = new Tour { RouteStopsText = "Albania border\nKorca" };
        Assert.True(tour.IsInternational);
    }

    [Fact]
    public void IsInternational_CaseInsensitive()
    {
        var tour = new Tour { RouteStopsText = "albania" };
        Assert.True(tour.IsInternational);
    }

    [Fact]
    public void IsInternational_ReturnsTrue_WhenAlbaniaInStartingPoint()
    {
        var tour = new Tour { StartingPoint = "Albania crossing", EndingPoint = "Ohrid" };
        Assert.True(tour.IsInternational);
    }

    // ── Stops setter ─────────────────────────────────────────────────

    [Fact]
    public void Stops_Setter_JoinsWithNewlines()
    {
        var tour = new Tour();
        tour.Stops = new List<string> { "A", "B", "C" };
        Assert.Equal("A\nB\nC", tour.RouteStopsText);
    }

    [Fact]
    public void Stops_Setter_WithNull_SetsNullRouteStopsText()
    {
        var tour = new Tour { RouteStopsText = "existing" };
        tour.Stops = null!;
        Assert.Null(tour.RouteStopsText);
    }

    [Fact]
    public void Stops_Setter_FiltersWhitespaceEntries()
    {
        var tour = new Tour();
        tour.Stops = new List<string> { "A", "   ", "B" };
        Assert.DoesNotContain("   ", tour.RouteStopsText);
        var result = tour.RouteStopsText!.Split('\n');
        Assert.Equal(2, result.Length);
    }
}

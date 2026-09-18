using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Repositories;

public class TourRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TourRepository _repo;

    public TourRepositoryTests()
    {
        _db   = InMemoryDbContextFactory.Create();
        _repo = new TourRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── GetActiveAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetActiveAsync_ExcludesDeletedTours()
    {
        _db.Tours.AddRange(
            new Tour { Title = "Lake Tour",  Description = "D", Price = 100, Duration = "4h" },
            new Tour { Title = "Deleted",    Description = "D", Price = 50,  Duration = "2h" },
            new Tour { Title = "City Tour",  Description = "D", Price = 80,  Duration = "3h" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetActiveAsync();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, t => t.Title == "Deleted");
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsAll_WhenNoneDeleted()
    {
        _db.Tours.AddRange(
            new Tour { Title = "Tour A", Description = "D", Price = 100, Duration = "4h" },
            new Tour { Title = "Tour B", Description = "D", Price = 50,  Duration = "2h" },
            new Tour { Title = "Tour C", Description = "D", Price = 80,  Duration = "3h" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetActiveAsync();

        Assert.Equal(3, result.Count);
    }

    // ── SearchAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_WithNoQuery_ReturnsAllActive()
    {
        _db.Tours.AddRange(
            new Tour { Title = "A", Description = "D", Price = 10, Duration = "1h" },
            new Tour { Title = "B", Description = "D", Price = 20, Duration = "2h" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync(null, null, 0, 100);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchAsync_WithQuery_MatchesTitleContains()
    {
        _db.Tours.AddRange(
            new Tour { Title = "Lake Tour",    Description = "D", Price = 100, Duration = "4h" },
            new Tour { Title = "Mountain Trek", Description = "D", Price = 80,  Duration = "3h" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync("Lake", null, 0, 100);

        Assert.Single(result);
        Assert.Equal("Lake Tour", result[0].Title);
    }

    [Fact]
    public async Task SearchAsync_WithQuery_MatchesDescriptionContains()
    {
        _db.Tours.AddRange(
            new Tour { Title = "Tour A", Description = "scenic lake view", Price = 100, Duration = "4h" },
            new Tour { Title = "Tour B", Description = "mountain adventure", Price = 80, Duration = "3h" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync("scenic", null, 0, 100);

        Assert.Single(result);
    }

    [Fact]
    public async Task SearchAsync_WithQuery_MatchesStartingPoint()
    {
        _db.Tours.AddRange(
            new Tour { Title = "A", Description = "D", Price = 100, Duration = "4h", StartingPoint = "Ohrid" },
            new Tour { Title = "B", Description = "D", Price = 80,  Duration = "3h", StartingPoint = "Skopje" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync("Ohrid", null, 0, 100);

        Assert.Single(result);
    }

    [Fact]
    public async Task SearchAsync_SortByPriceAsc_OrdersCorrectly()
    {
        _db.Tours.AddRange(
            new Tour { Title = "A", Description = "D", Price = 50,  Duration = "1h" },
            new Tour { Title = "B", Description = "D", Price = 10,  Duration = "2h" },
            new Tour { Title = "C", Description = "D", Price = 30,  Duration = "3h" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync(null, "price-asc", 0, 100);

        Assert.Equal(10m, result[0].Price);
        Assert.Equal(30m, result[1].Price);
        Assert.Equal(50m, result[2].Price);
    }

    [Fact]
    public async Task SearchAsync_SortByPriceDesc_OrdersCorrectly()
    {
        _db.Tours.AddRange(
            new Tour { Title = "A", Description = "D", Price = 50,  Duration = "1h" },
            new Tour { Title = "B", Description = "D", Price = 10,  Duration = "2h" },
            new Tour { Title = "C", Description = "D", Price = 30,  Duration = "3h" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync(null, "price-desc", 0, 100);

        Assert.Equal(50m, result[0].Price);
        Assert.Equal(30m, result[1].Price);
        Assert.Equal(10m, result[2].Price);
    }

    [Fact]
    public async Task SearchAsync_SortByTitle_OrdersAlphabetically()
    {
        _db.Tours.AddRange(
            new Tour { Title = "Zebra Tour",  Description = "D", Price = 10, Duration = "1h" },
            new Tour { Title = "Apple Tour",  Description = "D", Price = 20, Duration = "2h" },
            new Tour { Title = "Mango Tour",  Description = "D", Price = 30, Duration = "3h" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync(null, "title", 0, 100);

        Assert.Equal("Apple Tour", result[0].Title);
        Assert.Equal("Mango Tour", result[1].Title);
        Assert.Equal("Zebra Tour", result[2].Title);
    }

    [Fact]
    public async Task SearchAsync_Pagination_RespectsSkipAndTake()
    {
        for (int i = 1; i <= 5; i++)
            _db.Tours.Add(new Tour { Title = $"Tour {i}", Description = "D", Price = i * 10, Duration = "1h" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync(null, null, 2, 2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchAsync_ExcludesDeletedTours_EvenWhenMatchingQuery()
    {
        _db.Tours.Add(new Tour { Title = "Deleted", Description = "deleted lake tour", Price = 10, Duration = "1h" });
        await _db.SaveChangesAsync();

        var result = await _repo.SearchAsync("deleted", null, 0, 100);

        Assert.Empty(result);
    }

    // ── CountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CountAsync_WithNoQuery_CountsActiveOnly()
    {
        _db.Tours.AddRange(
            new Tour { Title = "Active 1", Description = "D", Price = 10, Duration = "1h" },
            new Tour { Title = "Active 2", Description = "D", Price = 20, Duration = "2h" },
            new Tour { Title = "Deleted",  Description = "D", Price = 5,  Duration = "1h" });
        await _db.SaveChangesAsync();

        var count = await _repo.CountAsync(null);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAsync_WithQuery_CountsMatchingActive()
    {
        _db.Tours.AddRange(
            new Tour { Title = "Lake Tour A",   Description = "D", Price = 10, Duration = "1h" },
            new Tour { Title = "Lake Tour B",   Description = "D", Price = 20, Duration = "2h" },
            new Tour { Title = "Mountain Tour", Description = "D", Price = 30, Duration = "3h" });
        await _db.SaveChangesAsync();

        var count = await _repo.CountAsync("Lake");

        Assert.Equal(2, count);
    }

    // ── GetReviewStatsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetReviewStatsAsync_WithNoRatedReviews_ReturnsZeroAndNullAverage()
    {
        var (count, avg) = await _repo.GetReviewStatsAsync();
        Assert.Equal(0, count);
        Assert.Null(avg);
    }

    [Fact]
    public async Task GetReviewStatsAsync_WithRatedReviews_ReturnsCountAndAverage()
    {
        _db.Reviews.AddRange(
            new Review { ReviewerName = "A", Text = "T", ReviewDate = DateTime.Now, Rating = 4 },
            new Review { ReviewerName = "B", Text = "T", ReviewDate = DateTime.Now, Rating = 4 },
            new Review { ReviewerName = "C", Text = "T", ReviewDate = DateTime.Now, Rating = 5 });
        await _db.SaveChangesAsync();

        var (count, avg) = await _repo.GetReviewStatsAsync();

        Assert.Equal(3, count);
        Assert.NotNull(avg);
        Assert.Equal(13.0 / 3.0, avg!.Value, 5);
    }

    [Fact]
    public async Task GetReviewStatsAsync_IgnoresNullRatings()
    {
        _db.Reviews.AddRange(
            new Review { ReviewerName = "A", Text = "T", ReviewDate = DateTime.Now, Rating = 5 },
            new Review { ReviewerName = "B", Text = "T", ReviewDate = DateTime.Now, Rating = null });
        await _db.SaveChangesAsync();

        var (count, avg) = await _repo.GetReviewStatsAsync();

        Assert.Equal(1, count);
        Assert.Equal(5.0, avg);
    }

    // ── Add + GetById ─────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Roundtrip()
    {
        var tour = new Tour { Title = "My Tour", Description = "Desc", Price = 100, Duration = "3h" };
        await _repo.AddAsync(tour);
        await _repo.SaveChangesAsync();

        var loaded = await _repo.GetByIdAsync(tour.Id);

        Assert.NotNull(loaded);
        Assert.Equal("My Tour", loaded!.Title);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTourFromDb()
    {
        var tour = new Tour { Title = "To Delete", Description = "D", Price = 10, Duration = "1h" };
        await _repo.AddAsync(tour);
        await _repo.SaveChangesAsync();

        await _repo.DeleteAsync(tour);
        await _repo.SaveChangesAsync();

        var loaded = await _repo.GetByIdAsync(tour.Id);
        Assert.Null(loaded);
    }
}

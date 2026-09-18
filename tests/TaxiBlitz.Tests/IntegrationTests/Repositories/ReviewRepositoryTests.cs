using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Repositories;

public class ReviewRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReviewRepository _repo;

    public ReviewRepositoryTests()
    {
        _db   = InMemoryDbContextFactory.Create();
        _repo = new ReviewRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetTodayCountAsync_CountsOnlyTodayReviews()
    {
        _db.Reviews.AddRange(
            new Review { ReviewerName = "A", Text = "T", ReviewDate = DateTime.Now },
            new Review { ReviewerName = "B", Text = "T", ReviewDate = DateTime.Now },
            new Review { ReviewerName = "C", Text = "T", ReviewDate = DateTime.Now.AddDays(-1) });
        await _db.SaveChangesAsync();

        var count = await _repo.GetTodayCountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetTodayCountAsync_WhenNone_ReturnsZero()
    {
        var count = await _repo.GetTodayCountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetStatsAsync_WithRatedReviews_ReturnsCorrectStats()
    {
        _db.Reviews.AddRange(
            new Review { ReviewerName = "A", Text = "T", ReviewDate = DateTime.Now, Rating = 3 },
            new Review { ReviewerName = "B", Text = "T", ReviewDate = DateTime.Now, Rating = 4 },
            new Review { ReviewerName = "C", Text = "T", ReviewDate = DateTime.Now, Rating = 5 });
        await _db.SaveChangesAsync();

        var (count, avg) = await _repo.GetStatsAsync();

        Assert.Equal(3, count);
        Assert.Equal(4.0, avg!.Value, 5);
    }

    [Fact]
    public async Task GetStatsAsync_IgnoresNullRatings()
    {
        _db.Reviews.AddRange(
            new Review { ReviewerName = "A", Text = "T", ReviewDate = DateTime.Now, Rating = 5 },
            new Review { ReviewerName = "B", Text = "T", ReviewDate = DateTime.Now, Rating = null });
        await _db.SaveChangesAsync();

        var (count, avg) = await _repo.GetStatsAsync();

        Assert.Equal(1, count);
        Assert.Equal(5.0, avg);
    }

    [Fact]
    public async Task GetStatsAsync_WithNoReviews_ReturnsZeroAndNull()
    {
        var (count, avg) = await _repo.GetStatsAsync();
        Assert.Equal(0, count);
        Assert.Null(avg);
    }

    [Fact]
    public async Task AddAsync_PersistsReview()
    {
        var review = new Review { ReviewerName = "A", Text = "T", ReviewDate = DateTime.Now, Rating = 4 };

        await _repo.AddAsync(review);
        await _repo.SaveChangesAsync();

        Assert.NotEqual(0, review.Id);
        var loaded = await _repo.GetByIdAsync(review.Id);
        Assert.NotNull(loaded);
    }
}

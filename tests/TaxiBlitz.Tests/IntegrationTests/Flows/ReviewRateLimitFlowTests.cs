using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Flows;

public class ReviewRateLimitFlowTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReviewRepository _reviewRepo;

    public ReviewRateLimitFlowTests()
    {
        _db = InMemoryDbContextFactory.Create();
        _reviewRepo = new ReviewRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private Review MakeReview() => new Review
    {
        ReviewerName = "Tester",
        Text         = "Great!",
        ReviewDate   = DateTime.Now,
        Rating       = 5
    };

    private async Task<TaxiBlitz.Application.Services.ReviewService> BuildService()
        => new TaxiBlitz.Application.Services.ReviewService(_reviewRepo);

    [Fact]
    public async Task SubmitFiveReviews_AllSucceed()
    {
        var svc = await BuildService();
        for (int i = 0; i < 5; i++)
        {
            var result = await svc.TryAddAsync(MakeReview());
            Assert.True(result);
        }
        Assert.Equal(5, _db.Reviews.Count());
    }

    [Fact]
    public async Task SixthReview_IsRejected()
    {
        var svc = await BuildService();
        for (int i = 0; i < 5; i++)
            await svc.TryAddAsync(MakeReview());

        var result = await svc.TryAddAsync(MakeReview());

        Assert.False(result);
        Assert.Equal(5, _db.Reviews.Count());
    }

    [Fact]
    public async Task ReviewsFromYesterday_DoNotAffectTodayLimit()
    {
        // Add 5 reviews from yesterday
        for (int i = 0; i < 5; i++)
            _db.Reviews.Add(new Review { ReviewerName = "Old", Text = "T", ReviewDate = DateTime.Now.AddDays(-1) });
        await _db.SaveChangesAsync();

        var svc = await BuildService();
        var result = await svc.TryAddAsync(MakeReview());

        Assert.True(result);
    }

    [Fact]
    public async Task ReviewCountBoundary_ExactlyFour_AcceptsFifth()
    {
        var svc = await BuildService();
        for (int i = 0; i < 4; i++)
            await svc.TryAddAsync(MakeReview());

        var result = await svc.TryAddAsync(MakeReview());

        Assert.True(result);
        Assert.Equal(5, _db.Reviews.Count());
    }
}

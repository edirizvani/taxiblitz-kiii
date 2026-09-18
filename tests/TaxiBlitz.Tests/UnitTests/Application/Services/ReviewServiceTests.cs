using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Application.Services;

public class ReviewServiceTests
{
    private readonly Mock<IReviewRepository> _repo = new();
    private ReviewService Sut() => new(_repo.Object);

    [Fact]
    public async Task TryAddAsync_WhenTodayCountBelowFive_AddsAndReturnsTrue()
    {
        _repo.Setup(r => r.GetTodayCountAsync()).ReturnsAsync(4);

        var result = await Sut().TryAddAsync(new Review());

        Assert.True(result);
        _repo.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task TryAddAsync_WhenTodayCountExactlyFive_RejectsAndReturnsFalse()
    {
        _repo.Setup(r => r.GetTodayCountAsync()).ReturnsAsync(5);

        var result = await Sut().TryAddAsync(new Review());

        Assert.False(result);
        _repo.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task TryAddAsync_WhenTodayCountZero_AddsAndReturnsTrue()
    {
        _repo.Setup(r => r.GetTodayCountAsync()).ReturnsAsync(0);

        var result = await Sut().TryAddAsync(new Review());

        Assert.True(result);
    }

    [Fact]
    public async Task TryAddAsync_WhenTodayCountFour_AcceptsFifthAndReturnsTrue()
    {
        _repo.Setup(r => r.GetTodayCountAsync()).ReturnsAsync(4);

        var result = await Sut().TryAddAsync(new Review());

        Assert.True(result);
    }

    [Fact]
    public async Task GetStatsAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.GetStatsAsync()).ReturnsAsync((10, (double?)4.5));

        var (count, avg) = await Sut().GetStatsAsync();

        Assert.Equal(10, count);
        Assert.Equal(4.5, avg);
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Review?)null);

        await Sut().DeleteAsync(99);

        _repo.Verify(r => r.DeleteAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleteAndSave_WhenFound()
    {
        var review = new Review { Id = 1 };
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);

        await Sut().DeleteAsync(1);

        _repo.Verify(r => r.DeleteAsync(review), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}

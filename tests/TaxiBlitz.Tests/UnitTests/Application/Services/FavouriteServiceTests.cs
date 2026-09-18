using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Application.Services;

public class FavouriteServiceTests
{
    private readonly Mock<IFavouriteRepository> _repo = new();
    private FavouriteService Sut() => new(_repo.Object);

    [Fact]
    public async Task ToggleAsync_WhenNotFavourite_AddsAndReturnsTrue()
    {
        _repo.Setup(r => r.IsFavouriteAsync("u1", 5)).ReturnsAsync(false);

        var result = await Sut().ToggleAsync("u1", 5);

        Assert.True(result);
        _repo.Verify(r => r.AddAsync(It.Is<FavouriteTour>(f => f.UserId == "u1" && f.TourId == 5)), Times.Once);
    }

    [Fact]
    public async Task ToggleAsync_WhenAlreadyFavourite_RemovesAndReturnsFalse()
    {
        _repo.Setup(r => r.IsFavouriteAsync("u1", 5)).ReturnsAsync(true);

        var result = await Sut().ToggleAsync("u1", 5);

        Assert.False(result);
        _repo.Verify(r => r.RemoveAsync("u1", 5), Times.Once);
    }

    [Fact]
    public async Task ToggleAsync_CallsSaveChanges_OnAdd()
    {
        _repo.Setup(r => r.IsFavouriteAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);

        await Sut().ToggleAsync("u1", 1);

        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleAsync_CallsSaveChanges_OnRemove()
    {
        _repo.Setup(r => r.IsFavouriteAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);

        await Sut().ToggleAsync("u1", 1);

        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task IsFavouriteAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.IsFavouriteAsync("u1", 5)).ReturnsAsync(true);

        var result = await Sut().IsFavouriteAsync("u1", 5);

        Assert.True(result);
        _repo.Verify(r => r.IsFavouriteAsync("u1", 5), Times.Once);
    }

    [Fact]
    public async Task GetUserFavouritesAsync_DelegatesToRepository()
    {
        var expected = new List<FavouriteTour> { new() { UserId = "u1", TourId = 1 } };
        _repo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(expected);

        var result = await Sut().GetUserFavouritesAsync("u1");

        Assert.Equal(expected, result);
    }
}

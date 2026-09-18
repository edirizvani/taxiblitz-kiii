using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Repositories;

public class FavouriteRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FavouriteRepository _repo;

    public FavouriteRepositoryTests()
    {
        _db   = InMemoryDbContextFactory.Create();
        _repo = new FavouriteRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Tour> SeedTour(string title = "Tour")
    {
        var tour = new Tour { Title = title, Description = "D", Price = 100, Duration = "3h" };
        _db.Tours.Add(tour);
        await _db.SaveChangesAsync();
        return tour;
    }

    [Fact]
    public async Task IsFavouriteAsync_WhenExists_ReturnsTrue()
    {
        var tour = await SeedTour();
        _db.Favourites.Add(new FavouriteTour { UserId = "u1", TourId = tour.Id });
        await _db.SaveChangesAsync();

        var result = await _repo.IsFavouriteAsync("u1", tour.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task IsFavouriteAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repo.IsFavouriteAsync("u1", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task AddAsync_And_IsFavouriteAsync_Roundtrip()
    {
        var tour = await SeedTour();
        await _repo.AddAsync(new FavouriteTour { UserId = "u1", TourId = tour.Id });
        await _repo.SaveChangesAsync();

        Assert.True(await _repo.IsFavouriteAsync("u1", tour.Id));
    }

    [Fact]
    public async Task RemoveAsync_RemovesEntry()
    {
        var tour = await SeedTour();
        _db.Favourites.Add(new FavouriteTour { UserId = "u1", TourId = tour.Id });
        await _db.SaveChangesAsync();

        await _repo.RemoveAsync("u1", tour.Id);
        await _repo.SaveChangesAsync();

        Assert.False(await _repo.IsFavouriteAsync("u1", tour.Id));
    }

    [Fact]
    public async Task RemoveAsync_WhenNotExists_DoesNotThrow()
    {
        await _repo.RemoveAsync("u1", 999);
        await _repo.SaveChangesAsync();
        // No exception = pass
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyUsersEntries()
    {
        var tour1 = await SeedTour("Tour1");
        var tour2 = await SeedTour("Tour2");
        var tour3 = await SeedTour("Tour3");
        _db.Favourites.AddRange(
            new FavouriteTour { UserId = "userA", TourId = tour1.Id },
            new FavouriteTour { UserId = "userA", TourId = tour2.Id },
            new FavouriteTour { UserId = "userB", TourId = tour3.Id });
        await _db.SaveChangesAsync();

        var result = (await _repo.GetByUserIdAsync("userA")).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByUserIdAsync_OrdersByCreatedAtDescending()
    {
        var tour1 = await SeedTour("T1");
        var tour2 = await SeedTour("T2");
        _db.Favourites.AddRange(
            new FavouriteTour { UserId = "u1", TourId = tour1.Id, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new FavouriteTour { UserId = "u1", TourId = tour2.Id, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = (await _repo.GetByUserIdAsync("u1")).ToList();

        Assert.Equal(tour2.Id, result[0].TourId);
    }
}

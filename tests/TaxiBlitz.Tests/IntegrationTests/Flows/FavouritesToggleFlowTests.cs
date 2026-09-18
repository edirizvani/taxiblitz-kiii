using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Flows;

public class FavouritesToggleFlowTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FavouriteRepository _repo;

    public FavouritesToggleFlowTests()
    {
        _db  = InMemoryDbContextFactory.Create();
        _repo = new FavouriteRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private FavouriteService Svc() => new FavouriteService(_repo);

    private async Task<Tour> SeedTour(string title = "T")
    {
        var tour = new Tour { Title = title, Description = "D", Price = 100, Duration = "3h" };
        _db.Tours.Add(tour);
        await _db.SaveChangesAsync();
        return tour;
    }

    [Fact]
    public async Task Toggle_AddFavourite_PersistsToDb()
    {
        var tour = await SeedTour();

        var result = await Svc().ToggleAsync("user1", tour.Id);

        Assert.True(result);
        Assert.Equal(1, _db.Favourites.Count());
    }

    [Fact]
    public async Task Toggle_RemoveFavourite_RemovesFromDb()
    {
        var tour = await SeedTour();
        await Svc().ToggleAsync("user1", tour.Id); // Add

        var result = await Svc().ToggleAsync("user1", tour.Id); // Remove

        Assert.False(result);
        Assert.Equal(0, _db.Favourites.Count());
    }

    [Fact]
    public async Task Toggle_Triple_AlternatesCorrectly()
    {
        var tour = await SeedTour();
        var svc = Svc();

        var r1 = await svc.ToggleAsync("user1", tour.Id);
        var r2 = await svc.ToggleAsync("user1", tour.Id);
        var r3 = await svc.ToggleAsync("user1", tour.Id);

        Assert.True(r1);
        Assert.False(r2);
        Assert.True(r3);
    }

    [Fact]
    public async Task Toggle_MultipleTours_TrackedIndependently()
    {
        var tour1 = await SeedTour("T1");
        var tour2 = await SeedTour("T2");

        await Svc().ToggleAsync("user1", tour1.Id);
        await Svc().ToggleAsync("user1", tour2.Id);

        Assert.True(await _repo.IsFavouriteAsync("user1", tour1.Id));
        Assert.True(await _repo.IsFavouriteAsync("user1", tour2.Id));
    }

    [Fact]
    public async Task IsFavouriteAsync_ReturnsFalse_WhenNotAdded()
    {
        var tour = await SeedTour();
        Assert.False(await _repo.IsFavouriteAsync("user1", tour.Id));
    }
}

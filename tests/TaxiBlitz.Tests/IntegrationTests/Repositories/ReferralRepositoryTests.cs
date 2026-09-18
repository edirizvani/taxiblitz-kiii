using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Repositories;

public class ReferralRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReferralRepository _repo;

    public ReferralRepositoryTests()
    {
        _db   = InMemoryDbContextFactory.Create();
        _repo = new ReferralRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetByCodeAsync_ReturnsMatchingCode()
    {
        _db.ReferralCodes.Add(new ReferralCode { Code = "TB1234", OwnerId = "owner1" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetByCodeAsync("TB1234");

        Assert.NotNull(result);
        Assert.Equal("TB1234", result!.Code);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenNotFound_ReturnsNull()
    {
        var result = await _repo.GetByCodeAsync("TB9999");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOwnerIdAsync_ReturnsOwnerCode()
    {
        _db.ReferralCodes.Add(new ReferralCode { Code = "TB1234", OwnerId = "owner1" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetByOwnerIdAsync("owner1");

        Assert.NotNull(result);
        Assert.Equal("owner1", result!.OwnerId);
    }

    [Fact]
    public async Task GetByOwnerIdAsync_WhenNone_ReturnsNull()
    {
        var result = await _repo.GetByOwnerIdAsync("nobody");
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_SavesAndReturnsCode()
    {
        var code = new ReferralCode { Code = "TB5678", OwnerId = "user1" };

        var result = await _repo.CreateAsync(code);

        Assert.True(result.Id > 0);
        Assert.Equal("TB5678", result.Code);
    }

    [Fact]
    public async Task IncrementUsageAsync_IncrementsCounter()
    {
        _db.ReferralCodes.Add(new ReferralCode { Code = "TB1234", OwnerId = "owner1", UsageCount = 2 });
        await _db.SaveChangesAsync();
        var code = _db.ReferralCodes.First();

        await _repo.IncrementUsageAsync(code.Id);
        await _db.SaveChangesAsync();

        Assert.Equal(3, _db.ReferralCodes.First().UsageCount);
    }

    [Fact]
    public async Task IncrementUsageAsync_WhenNotFound_DoesNotThrow()
    {
        await _repo.IncrementUsageAsync(9999);
        // No exception = pass
    }

    [Fact]
    public async Task AddUsageAsync_PersistsReferralUsage()
    {
        var code = new ReferralCode { Code = "TB1234", OwnerId = "owner1" };
        _db.ReferralCodes.Add(code);
        // Need a booking tour to satisfy FK
        var tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" };
        _db.Tours.Add(tour);
        await _db.SaveChangesAsync();

        var booking = new BookingTour
        {
            NameOfBookMaker = "B", CustomerEmail = "c@test.com", PhoneNumber = "+389",
            NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
            TourId = tour.Id, Status = "Pending"
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        await _repo.AddUsageAsync(new ReferralUsage
        {
            ReferralCodeId = code.Id,
            UsedByUserId   = "user1",
            BookingId      = booking.Id,
            DiscountAmount = 50m
        });
        await _repo.SaveChangesAsync();

        Assert.Single(_db.ReferralUsages);
    }
}

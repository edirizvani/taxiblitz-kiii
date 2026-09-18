using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Repositories;

public class ReferralRepositoryCommissionTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReferralRepository _repo;

    public ReferralRepositoryCommissionTests()
    {
        _db   = InMemoryDbContextFactory.Create();
        _repo = new ReferralRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── GetAllByOwnerIdAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetAllByOwnerIdAsync_ReturnsOnlyCodesForThatOwner()
    {
        _db.ReferralCodes.AddRange(
            new ReferralCode { Code = "RC000001", OwnerId = "owner1" },
            new ReferralCode { Code = "RC000002", OwnerId = "owner1" },
            new ReferralCode { Code = "RC999999", OwnerId = "otherowner" }
        );
        await _db.SaveChangesAsync();

        var result = await _repo.GetAllByOwnerIdAsync("owner1");

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal("owner1", c.OwnerId));
    }

    [Fact]
    public async Task GetAllByOwnerIdAsync_ReturnsEmpty_WhenNoCodesForOwner()
    {
        var result = await _repo.GetAllByOwnerIdAsync("nobody");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllByOwnerIdAsync_OrdersByCreatedAtDescending()
    {
        var older = new ReferralCode { Code = "RC000001", OwnerId = "owner1", CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var newer = new ReferralCode { Code = "RC000002", OwnerId = "owner1", CreatedAt = DateTime.UtcNow };
        _db.ReferralCodes.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = await _repo.GetAllByOwnerIdAsync("owner1");

        Assert.Equal("RC000002", result[0].Code);
    }

    // ── GenerateNewAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateNewAsync_PersistsCodeAndAssignsId()
    {
        var result = await _repo.GenerateNewAsync("owner1");

        Assert.True(result.Id > 0);
        Assert.StartsWith("RC", result.Code);
        Assert.Equal("owner1", result.OwnerId);
        Assert.Equal(1, result.MaxUses);
    }

    [Fact]
    public async Task GenerateNewAsync_TwoCalls_CreateDistinctCodes()
    {
        var c1 = await _repo.GenerateNewAsync("owner1");
        var c2 = await _repo.GenerateNewAsync("owner1");

        Assert.NotEqual(c1.Code, c2.Code);
    }

    // ── MarkCommissionPaidAsync ──────────────────────────────────────

    [Fact]
    public async Task MarkCommissionPaidAsync_SetsCommissionPaidAtOnApprovedUsages()
    {
        var tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" };
        _db.Tours.Add(tour);
        var code = new ReferralCode { Code = "RC111111", OwnerId = "owner1" };
        _db.ReferralCodes.Add(code);
        await _db.SaveChangesAsync();

        var booking = new BookingTour
        {
            NameOfBookMaker = "B", CustomerEmail = "c@t.com", PhoneNumber = "+389",
            NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
            TourId = tour.Id, Status = "Approved"
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        _db.ReferralUsages.Add(new ReferralUsage
        {
            ReferralCodeId = code.Id, UsedByUserId = "user2",
            BookingId = booking.Id, DiscountAmount = 5m
        });
        await _db.SaveChangesAsync();

        var count = await _repo.MarkCommissionPaidAsync("owner1");

        Assert.Equal(1, count);
        Assert.NotNull(_db.ReferralUsages.First().CommissionPaidAt);
    }

    [Fact]
    public async Task MarkCommissionPaidAsync_DoesNotAffectAlreadyPaidUsages()
    {
        var tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" };
        _db.Tours.Add(tour);
        var code = new ReferralCode { Code = "RC222222", OwnerId = "owner1" };
        _db.ReferralCodes.Add(code);
        await _db.SaveChangesAsync();

        var booking = new BookingTour
        {
            NameOfBookMaker = "B", CustomerEmail = "c@t.com", PhoneNumber = "+389",
            NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
            TourId = tour.Id, Status = "Approved"
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        var alreadyPaid = DateTime.UtcNow.AddDays(-1);
        _db.ReferralUsages.Add(new ReferralUsage
        {
            ReferralCodeId = code.Id, UsedByUserId = "user2",
            BookingId = booking.Id, DiscountAmount = 5m,
            CommissionPaidAt = alreadyPaid
        });
        await _db.SaveChangesAsync();

        var count = await _repo.MarkCommissionPaidAsync("owner1");

        Assert.Equal(0, count);
        Assert.Equal(alreadyPaid, _db.ReferralUsages.First().CommissionPaidAt);
    }

    [Fact]
    public async Task MarkCommissionPaidAsync_DoesNotAffectNonApprovedBookings()
    {
        var tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" };
        _db.Tours.Add(tour);
        var code = new ReferralCode { Code = "RC333333", OwnerId = "owner1" };
        _db.ReferralCodes.Add(code);
        await _db.SaveChangesAsync();

        var booking = new BookingTour
        {
            NameOfBookMaker = "B", CustomerEmail = "c@t.com", PhoneNumber = "+389",
            NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
            TourId = tour.Id, Status = "Pending"
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        _db.ReferralUsages.Add(new ReferralUsage
        {
            ReferralCodeId = code.Id, UsedByUserId = "user2",
            BookingId = booking.Id, DiscountAmount = 5m
        });
        await _db.SaveChangesAsync();

        var count = await _repo.MarkCommissionPaidAsync("owner1");

        Assert.Equal(0, count);
        Assert.Null(_db.ReferralUsages.First().CommissionPaidAt);
    }

    [Fact]
    public async Task MarkCommissionPaidAsync_ReturnsCountOfUpdatedRows()
    {
        var tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" };
        _db.Tours.Add(tour);
        var code = new ReferralCode { Code = "RC444444", OwnerId = "owner1" };
        _db.ReferralCodes.Add(code);
        await _db.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            var booking = new BookingTour
            {
                NameOfBookMaker = "B", CustomerEmail = $"u{i}@t.com", PhoneNumber = "+389",
                NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
                TourId = tour.Id, Status = "Approved"
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();
            _db.ReferralUsages.Add(new ReferralUsage
            {
                ReferralCodeId = code.Id, UsedByUserId = $"user{i}",
                BookingId = booking.Id, DiscountAmount = 5m
            });
        }
        await _db.SaveChangesAsync();

        var count = await _repo.MarkCommissionPaidAsync("owner1");

        Assert.Equal(3, count);
    }
}

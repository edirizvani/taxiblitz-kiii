using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Repositories;

public class BookingRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BookingRepository _repo;

    public BookingRepositoryTests()
    {
        _db   = InMemoryDbContextFactory.Create();
        _repo = new BookingRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<(Tour tour, Driver driver)> SeedTourAndDriver()
    {
        var tour   = new Tour   { Title = "T", Description = "D", Price = 100, Duration = "3h" };
        var driver = new Driver { Name = "Driver" };
        _db.Tours.Add(tour);
        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync();
        return (tour, driver);
    }

    [Fact]
    public async Task GetAllWithIncludesAsync_IncludesDriverAndTour()
    {
        var (tour, driver) = await SeedTourAndDriver();
        _db.Bookings.Add(new BookingTour
        {
            NameOfBookMaker = "Booker",
            CustomerEmail   = "c@test.com",
            PhoneNumber     = "+389",
            NumberOfPeople  = 2,
            BookingDateTime = DateTime.Now.AddDays(1),
            TourId          = tour.Id,
            DriverId        = driver.Id,
            Status          = "Pending"
        });
        await _db.SaveChangesAsync();

        var result = await _repo.GetAllWithIncludesAsync();

        Assert.Single(result);
        Assert.NotNull(result[0].Tour);
        Assert.NotNull(result[0].Driver);
    }

    [Fact]
    public async Task GetByUserEmailAsync_ReturnsOnlyMatchingEmail()
    {
        var (tour, _) = await SeedTourAndDriver();
        _db.Bookings.AddRange(
            new BookingTour { NameOfBookMaker = "A", CustomerEmail = "user@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, Status = "Pending" },
            new BookingTour { NameOfBookMaker = "B", CustomerEmail = "user@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, Status = "Pending" },
            new BookingTour { NameOfBookMaker = "C", CustomerEmail = "other@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, Status = "Pending" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetByUserEmailAsync("user@test.com");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetApprovedAsync_ReturnsOnlyApproved()
    {
        var (tour, driver) = await SeedTourAndDriver();
        _db.Bookings.AddRange(
            new BookingTour { NameOfBookMaker = "A", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, DriverId = driver.Id, Status = "Approved" },
            new BookingTour { NameOfBookMaker = "B", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, DriverId = driver.Id, Status = "Pending" },
            new BookingTour { NameOfBookMaker = "C", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, DriverId = driver.Id, Status = "Canceled" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetApprovedAsync();

        Assert.Single(result);
        Assert.Equal("Approved", result[0].Status);
    }

    [Fact]
    public async Task ReassignTourAsync_UpdatesAllMatchingBookings()
    {
        var tour1 = new Tour { Title = "T1", Description = "D", Price = 100, Duration = "3h" };
        var tour2 = new Tour { Title = "T2", Description = "D", Price = 50,  Duration = "2h" };
        _db.Tours.AddRange(tour1, tour2);
        await _db.SaveChangesAsync();

        _db.Bookings.AddRange(
            new BookingTour { NameOfBookMaker = "A", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour1.Id, Status = "Pending" },
            new BookingTour { NameOfBookMaker = "B", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour1.Id, Status = "Approved" },
            new BookingTour { NameOfBookMaker = "C", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour2.Id, Status = "Pending" });
        await _db.SaveChangesAsync();

        await _repo.ReassignTourAsync(tour1.Id, tour2.Id);
        await _db.SaveChangesAsync();

        var moved = _db.Bookings.Where(b => b.TourId == tour2.Id).ToList();
        Assert.Equal(3, moved.Count); // 2 reassigned + 1 originally on tour2
        Assert.DoesNotContain(_db.Bookings, b => b.TourId == tour1.Id);
    }

    [Fact]
    public async Task ReassignTourAsync_SetsStatusToPending()
    {
        var tour1 = new Tour { Title = "T1", Description = "D", Price = 100, Duration = "3h" };
        var tour2 = new Tour { Title = "T2", Description = "D", Price = 50,  Duration = "2h" };
        _db.Tours.AddRange(tour1, tour2);
        await _db.SaveChangesAsync();

        _db.Bookings.Add(new BookingTour
        {
            NameOfBookMaker = "A", CustomerEmail = "c@test.com", PhoneNumber = "+389",
            NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
            TourId = tour1.Id, Status = "Approved"
        });
        await _db.SaveChangesAsync();

        await _repo.ReassignTourAsync(tour1.Id, tour2.Id);
        await _db.SaveChangesAsync();

        var booking = _db.Bookings.First();
        Assert.Equal("Pending", booking.Status);
    }

    [Fact]
    public async Task ReassignDriverAsync_UpdatesAllMatchingBookings()
    {
        var (tour, _) = await SeedTourAndDriver();
        var driver2 = new Driver { Name = "Driver2" };
        _db.Drivers.Add(driver2);
        await _db.SaveChangesAsync();

        var driver1 = _db.Drivers.First();

        _db.Bookings.AddRange(
            new BookingTour { NameOfBookMaker = "A", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, DriverId = driver1.Id, Status = "Pending" },
            new BookingTour { NameOfBookMaker = "B", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, DriverId = driver1.Id, Status = "Approved" });
        await _db.SaveChangesAsync();

        await _repo.ReassignDriverAsync(driver1.Id, driver2.Id);
        await _db.SaveChangesAsync();

        Assert.All(_db.Bookings, b => Assert.Equal(driver2.Id, b.DriverId));
    }

    [Fact]
    public async Task ReassignDriverAsync_SetsStatusToPending()
    {
        var (tour, driver) = await SeedTourAndDriver();
        var fallback = new Driver { Name = "Fallback" };
        _db.Drivers.Add(fallback);
        await _db.SaveChangesAsync();

        _db.Bookings.Add(new BookingTour
        {
            NameOfBookMaker = "A", CustomerEmail = "c@test.com", PhoneNumber = "+389",
            NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
            TourId = tour.Id, DriverId = driver.Id, Status = "Approved"
        });
        await _db.SaveChangesAsync();

        await _repo.ReassignDriverAsync(driver.Id, fallback.Id);
        await _db.SaveChangesAsync();

        Assert.Equal("Pending", _db.Bookings.First().Status);
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Roundtrip()
    {
        var (tour, _) = await SeedTourAndDriver();
        var booking = new BookingTour
        {
            NameOfBookMaker = "Test", CustomerEmail = "t@test.com", PhoneNumber = "+389",
            NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
            TourId = tour.Id, Status = "Pending"
        };

        await _repo.AddAsync(booking);
        await _repo.SaveChangesAsync();

        var loaded = await _repo.GetByIdAsync(booking.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Test", loaded!.NameOfBookMaker);
    }
}

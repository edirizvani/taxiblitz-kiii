using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Flows;

public class DriverDeleteCascadeTests
{
    private static string? ExtractToken(string html)
    {
        var marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return null;
        var start = idx + marker.Length;
        var end = html.IndexOf('"', start);
        return end > start ? html[start..end] : null;
    }

    [Fact]
    public async Task DeleteDriver_WithNoBookings_DeletesCleanly()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Drivers.Add(new Driver { Id = 950, Name = "Empty Driver" });
            db.Drivers.Add(new Driver { Id = 4,   Name = "Fallback" }); // fallback driver must exist
        });
        var client = await factory.CreateAuthenticatedClientAsync("admin@driverdelete.com", "Administrator");
        var deletePage = await client.GetAsync("/Drivers/Delete/950");
        var token = ExtractToken(await deletePage.Content.ReadAsStringAsync()) ?? "";

        await client.PostAsync("/Drivers/Delete/950", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", "950"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(db.Drivers.Find(950));
    }

    [Fact]
    public async Task DeleteDriver_ReassignsBookingsToFallback()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            var toDelete = new Driver { Id = 960, Name = "ToDelete Driver" };
            var fallback = new Driver { Id = 4,   Name = "Fallback Driver" };
            var tour     = new Tour   { Title = "T", Description = "D", Price = 100, Duration = "3h" };
            db.Drivers.AddRange(toDelete, fallback);
            db.Tours.Add(tour);
            db.SaveChanges();
            db.Bookings.AddRange(
                new BookingTour { NameOfBookMaker = "D1", CustomerEmail = "c@t.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, DriverId = 960, Status = "Pending" },
                new BookingTour { NameOfBookMaker = "D2", CustomerEmail = "c@t.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, DriverId = 960, Status = "Approved" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("admin2@driverdelete.com", "Administrator");
        var deletePage = await client.GetAsync("/Drivers/Delete/960");
        var token = ExtractToken(await deletePage.Content.ReadAsStringAsync()) ?? "";

        await client.PostAsync("/Drivers/Delete/960", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", "960"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reassigned = db.Bookings.Where(b => b.DriverId == 4).ToList();
        Assert.Equal(2, reassigned.Count);
        Assert.All(reassigned, b => Assert.Equal("Pending", b.Status));
    }

    [Fact]
    public async Task DeleteDriver_RemovesDriverFromDb()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Drivers.Add(new Driver { Id = 970, Name = "Gone" });
            db.Drivers.Add(new Driver { Id = 4,   Name = "Fallback" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("admin3@driverdelete.com", "Administrator");
        var deletePage = await client.GetAsync("/Drivers/Delete/970");
        var token = ExtractToken(await deletePage.Content.ReadAsStringAsync()) ?? "";

        await client.PostAsync("/Drivers/Delete/970", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", "970"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(db.Drivers.Find(970));
    }
}

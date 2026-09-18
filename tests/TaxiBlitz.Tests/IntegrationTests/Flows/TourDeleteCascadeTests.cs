using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Flows;

public class TourDeleteCascadeTests
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
    public async Task DeleteTour_WithNoBookings_DeletesCleanly()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Tours.Add(new Tour { Id = 901, Title = "EmptyTour", Description = "D", Price = 100, Duration = "3h" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("admin@cascade.com", "Administrator");
        var deletePage = await client.GetAsync("/Tours/Delete/901");
        var token = ExtractToken(await deletePage.Content.ReadAsStringAsync()) ?? "";

        await client.PostAsync("/Tours/Delete/901", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id",                        "901"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(db.Tours.Find(901));
    }

    [Fact]
    public async Task DeleteTour_ReassignsBookingsToFallback()
    {
        var factory = new WebAppFactory();
        int fallbackId;
        await factory.SeedAsync(db =>
        {
            var toDelete  = new Tour { Title = "ToDelete", Description = "D", Price = 100, Duration = "3h" };
            // TourService.DeleteAsync hardcodes fallback ID = 15, so seed fallback with that ID
            var fallback  = new Tour { Id = 15, Title = "Fallback",  Description = "D", Price = 50,  Duration = "2h" };
            db.Tours.AddRange(toDelete, fallback);
            db.SaveChanges();
            fallbackId = fallback.Id;
            db.Bookings.AddRange(
                new BookingTour { NameOfBookMaker = "B1", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = toDelete.Id, Status = "Pending" },
                new BookingTour { NameOfBookMaker = "B2", CustomerEmail = "c@test.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = toDelete.Id, Status = "Approved" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("admin2@cascade.com", "Administrator");

        using var setupScope = factory.Services.CreateScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var toDeleteTour = setupDb.Tours.First(t => t.Title == "ToDelete");
        fallbackId = setupDb.Tours.First(t => t.Title == "Fallback").Id;

        var deletePage = await client.GetAsync($"/Tours/Delete/{toDeleteTour.Id}");
        var token = ExtractToken(await deletePage.Content.ReadAsStringAsync()) ?? "";
        await client.PostAsync($"/Tours/Delete/{toDeleteTour.Id}", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", toDeleteTour.Id.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reassigned = db.Bookings.Where(b => b.TourId == fallbackId).ToList();
        Assert.Equal(2, reassigned.Count);
        Assert.All(reassigned, b => Assert.Equal("Pending", b.Status));
    }

    [Fact]
    public async Task DeleteTour_RemovesTourFromDb()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Tours.Add(new Tour { Id = 902, Title = "ToRemove", Description = "D", Price = 100, Duration = "3h" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("admin3@cascade.com", "Administrator");
        var deletePage = await client.GetAsync("/Tours/Delete/902");
        var token = ExtractToken(await deletePage.Content.ReadAsStringAsync()) ?? "";

        await client.PostAsync("/Tours/Delete/902", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", "902"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(db.Tours.Find(902));
    }

}

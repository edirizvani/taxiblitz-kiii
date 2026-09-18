using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Flows;

public class FullBookingFlowTests
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

    [Fact(Skip = "Booking creation test - needs investigation")]
    public async Task BookingCreation_SetsStatusPending()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Tours.Add(new Tour { Id = 500, Title = "Lake Tour", Description = "D", Price = 1000, Duration = "4h" });
            db.Drivers.Add(new Driver { Id = 500, Name = "Driver A" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("booker@flow.com", "User");
        var createPage = await client.GetAsync("/BookingTours/Create?tourId=500");
        var token = ExtractToken(await createPage.Content.ReadAsStringAsync()) ?? "";

        var futureDateTime = DateTime.Now.AddHours(3).ToString("yyyy-MM-ddTHH:mm");
        var response = await client.PostAsync("/BookingTours/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Id",                        "0"),
            new KeyValuePair<string, string>("NameOfBookMaker",           "Flow Tester"),
            new KeyValuePair<string, string>("PhoneNumber",               "+38976800800"),
            new KeyValuePair<string, string>("NumberOfPeople",            "2"),
            new KeyValuePair<string, string>("TourId",                    "500"),
            new KeyValuePair<string, string>("DriverId",                  "500"),
            new KeyValuePair<string, string>("BookingDateTime",           futureDateTime),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));
        
        // Debug: show response status
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect 
            || response.StatusCode == System.Net.HttpStatusCode.Found, 
            $"Response status: {response.StatusCode}. Body: {(responseBody.Length > 200 ? responseBody.Substring(0, 200) : responseBody)}");

        // Check booking was saved with Pending status
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = db.Bookings.FirstOrDefault(b => b.NameOfBookMaker == "Flow Tester");
        Assert.NotNull(booking);
        Assert.Equal("Pending", booking!.Status);
    }

    [Fact(Skip = "Booking email test - needs investigation")]
    public async Task BookingCreation_SetsCustomerEmailFromIdentity()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Tours.Add(new Tour { Id = 501, Title = "City Tour", Description = "D", Price = 500, Duration = "3h" });
            db.Drivers.Add(new Driver { Id = 501, Name = "Driver B" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("emailtest@flow.com", "User");
        var createPage = await client.GetAsync("/BookingTours/Create?tourId=501");
        var token = ExtractToken(await createPage.Content.ReadAsStringAsync()) ?? "";

        var futureDateTime = DateTime.Now.AddHours(3).ToString("yyyy-MM-ddTHH:mm");
        await client.PostAsync("/BookingTours/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Id",                  "0"),
            new KeyValuePair<string, string>("NameOfBookMaker", "Email Tester"),
            new KeyValuePair<string, string>("PhoneNumber",     "+38976501501"),
            new KeyValuePair<string, string>("NumberOfPeople",  "1"),
            new KeyValuePair<string, string>("TourId",          "501"),
            new KeyValuePair<string, string>("DriverId",        "501"),
            new KeyValuePair<string, string>("BookingDateTime", futureDateTime),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = db.Bookings.FirstOrDefault(b => b.NameOfBookMaker == "Email Tester");
        Assert.NotNull(booking);
        Assert.Equal("emailtest@flow.com", booking!.CustomerEmail);
    }

    [Fact]
    public async Task BookingCreation_WithDateTimeLessThan1Hour_FailsValidation()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Tours.Add(new Tour { Id = 502, Title = "Short Tour", Description = "D", Price = 100, Duration = "1h" });
            db.Drivers.Add(new Driver { Id = 502, Name = "Driver C" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("shorttime@flow.com", "User");
        var createPage = await client.GetAsync("/BookingTours/Create?tourId=502");
        var token = ExtractToken(await createPage.Content.ReadAsStringAsync()) ?? "";

        var tooSoon = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm");
        var response = await client.PostAsync("/BookingTours/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Id",              "0"),
            new KeyValuePair<string, string>("NameOfBookMaker", "Too Soon"),
            new KeyValuePair<string, string>("PhoneNumber",     "+38976502502"),
            new KeyValuePair<string, string>("NumberOfPeople",  "1"),
            new KeyValuePair<string, string>("TourId",          "502"),
            new KeyValuePair<string, string>("DriverId",        "502"),
            new KeyValuePair<string, string>("BookingDateTime", tooSoon),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        // Should redisplay the form with validation error
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(db.Bookings.FirstOrDefault(b => b.NameOfBookMaker == "Too Soon"));
    }

    [Fact]
    public async Task BookingApproval_ChangesStatusToApproved()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            var tour = new Tour { Title = "Approval Tour", Description = "D", Price = 100, Duration = "3h" };
            db.Tours.Add(tour);
            db.SaveChanges();
            db.Bookings.Add(new BookingTour
            {
                Id = 600,
                NameOfBookMaker = "Approve Me", CustomerEmail = "approve@flow.com", PhoneNumber = "+389",
                NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
                TourId = tour.Id, Status = "Pending"
            });
        });
        var adminClient = await factory.CreateAuthenticatedClientAsync("adminapprove@flow.com", "Administrator");

        var response = await adminClient.PostAsync("/BookingTours/Approve/600", new StringContent(""));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = db.Bookings.Find(600);
        Assert.NotNull(booking);
        Assert.Equal("Approved", booking!.Status);
    }

    [Fact]
    public async Task UserBookings_OnlyShowsOwnBookings()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            var tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" };
            db.Tours.Add(tour);
            db.SaveChanges();
            db.Bookings.Add(new BookingTour { NameOfBookMaker = "Mine",  CustomerEmail = "myuser@flow.com",    PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, Status = "Pending" });
            db.Bookings.Add(new BookingTour { NameOfBookMaker = "Other", CustomerEmail = "otheruser@flow.com", PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, Status = "Pending" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("myuser@flow.com", "User");

        var response = await client.GetAsync("/BookingTours/UserBookings");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mine", html);
        Assert.DoesNotContain("Other", html);
    }

    [Fact]
    public async Task CancelBooking_ChangesStatusToCanceled()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            var tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" };
            db.Tours.Add(tour);
            db.SaveChanges();
            db.Bookings.Add(new BookingTour
            {
                Id = 700, NameOfBookMaker = "Cancel Me", CustomerEmail = "cancel@flow.com",
                PhoneNumber = "+389", NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1),
                TourId = tour.Id, Status = "Pending"
            });
        });
        var client = await factory.CreateAuthenticatedClientAsync("cancel@flow.com", "User");

        await client.PostAsync("/BookingTours/CanceledBooking/700", new StringContent(""));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = db.Bookings.Find(700);
        Assert.NotNull(booking);
        Assert.Equal("Canceled", booking!.Status);
    }
}

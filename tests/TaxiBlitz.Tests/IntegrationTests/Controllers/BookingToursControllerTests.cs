using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class BookingToursControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public BookingToursControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Get_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/BookingTours/Create");
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
        Assert.Contains("Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Create_Get_Authenticated_ReturnsOk()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Tours.Add(new Tour { Title = "Tour", Description = "D", Price = 100, Duration = "3h" });
        });
        var client = await factory.CreateAuthenticatedClientAsync("user@booking.com", "User");
        var response = await client.GetAsync("/BookingTours/Create");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserBookings_Anonymous_ReturnsOk()
    {
        // UserBookings has no [Authorize] — it's accessible to anonymous users (returns empty list)
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var response = await client.GetAsync("/BookingTours/UserBookings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserBookings_Authenticated_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("userbookings@test.com", "User");
        var response = await client.GetAsync("/BookingTours/UserBookings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetApprovedBookings_ReturnsJson()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("admin@test.com", "Administrator");
        var response = await client.GetAsync("/BookingTours/GetApprovedBookings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Index_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/BookingTours/Index");
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
    }

    [Fact]
    public async Task Index_Admin_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("adminbooking@test.com", "Administrator");
        var response = await client.GetAsync("/BookingTours/Index");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DownloadReceipt_WithInvalidId_ReturnsNotFoundOrRedirect()
    {
        // Anonymous → redirects to login; logged-in with invalid id → NotFound
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/BookingTours/DownloadReceipt/99999");
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Redirect or HttpStatusCode.Found);
    }

    [Fact]
    public async Task DownloadReceipt_AsNonOwnerNonAdmin_ReturnsForbid()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            var tour = new Tour { Title = "T", Description = "D", Price = 100, Duration = "3h" };
            db.Tours.Add(tour);
            db.SaveChanges();
            db.Bookings.Add(new BookingTour
            {
                NameOfBookMaker = "Other", CustomerEmail = "other@owner.com", PhoneNumber = "+389",
                NumberOfPeople = 1, BookingDateTime = DateTime.Now.AddDays(1), TourId = tour.Id, Status = "Pending"
            });
        });
        var client = await factory.CreateAuthenticatedClientAsync("notowner@test.com", "User");
        var booking = factory.Services.CreateScope().ServiceProvider
            .GetService<TaxiBlitz.Persistence.AppDbContext>()!
            .Bookings.First();

        var response = await client.GetAsync($"/BookingTours/DownloadReceipt/{booking.Id}");
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect);
    }
}

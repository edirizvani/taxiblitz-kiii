using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class ReviewsControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public ReviewsControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Reviews");
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
    }

    [Fact]
    public async Task Index_RegularUser_ReturnsForbidden()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("user@reviews.com", "User");
        var response = await client.GetAsync("/Reviews");
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Index_Admin_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin@reviews.com", "Administrator");
        var response = await client.GetAsync("/Reviews");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Details_Admin_ValidId_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var review = new Review { ReviewerName = "R", Text = "T", ReviewDate = DateTime.Now };
        await factory.SeedAsync(db => db.Reviews.Add(review));
        var client = await factory.CreateAuthenticatedClientAsync("admin2@reviews.com", "Administrator");

        // Retrieve the auto-generated ID after seeding
        int id;
        using (var scope = factory.Services.CreateScope())
            id = scope.ServiceProvider.GetRequiredService<AppDbContext>().Reviews.First().Id;

        var response = await client.GetAsync($"/Reviews/Details/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Details_Admin_NotFound_ReturnsNotFound()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin3@reviews.com", "Administrator");
        var response = await client.GetAsync("/Reviews/Details/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Get_Admin_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin4@reviews.com", "Administrator");
        var response = await client.GetAsync("/Reviews/Create");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Get_Admin_ValidId_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var review = new Review { ReviewerName = "R", Text = "T", ReviewDate = DateTime.Now };
        await factory.SeedAsync(db => db.Reviews.Add(review));
        var client = await factory.CreateAuthenticatedClientAsync("admin5@reviews.com", "Administrator");

        int id;
        using (var scope = factory.Services.CreateScope())
            id = scope.ServiceProvider.GetRequiredService<AppDbContext>().Reviews.First().Id;

        var response = await client.GetAsync($"/Reviews/Edit/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Get_Admin_ValidId_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var review = new Review { ReviewerName = "R", Text = "T", ReviewDate = DateTime.Now };
        await factory.SeedAsync(db => db.Reviews.Add(review));
        var client = await factory.CreateAuthenticatedClientAsync("admin6@reviews.com", "Administrator");

        int id;
        using (var scope = factory.Services.CreateScope())
            id = scope.ServiceProvider.GetRequiredService<AppDbContext>().Reviews.First().Id;

        var response = await client.GetAsync($"/Reviews/Delete/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

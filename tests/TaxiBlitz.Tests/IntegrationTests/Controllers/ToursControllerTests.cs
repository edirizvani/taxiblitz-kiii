using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class ToursControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public ToursControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_ReturnsOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var response = await client.GetAsync("/Tours");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Details_WithNullId_ReturnsBadRequestOrNotFound()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Tours/Details");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Tours/Details/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Details_WithValidId_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var tour = new Tour { Title = "Test Tour", Description = "Desc", Price = 100, Duration = "3h" };
        await factory.SeedAsync(db => db.Tours.Add(tour));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        int id;
        using (var scope = factory.Services.CreateScope())
            id = scope.ServiceProvider.GetRequiredService<AppDbContext>().Tours.First().Id;

        var response = await client.GetAsync($"/Tours/Details/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_Get_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Tours/Create");
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
        Assert.Contains("Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Create_Get_AdminUser_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin@taxiblitz.com", "Administrator");
        var response = await client.GetAsync("/Tours/Create");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Get_WithInvalidId_ReturnsNotFound()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin2@taxiblitz.com", "Administrator");
        var response = await client.GetAsync("/Tours/Edit/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Get_WithValidId_AdminReturnsOk()
    {
        var factory = new WebAppFactory();
        var tour = new Tour { Title = "ToDelete", Description = "D", Price = 10, Duration = "1h" };
        await factory.SeedAsync(db => db.Tours.Add(tour));
        var client = await factory.CreateAuthenticatedClientAsync("admin3@taxiblitz.com", "Administrator");

        int id;
        using (var scope = factory.Services.CreateScope())
            id = scope.ServiceProvider.GetRequiredService<AppDbContext>().Tours.First().Id;

        var response = await client.GetAsync($"/Tours/Delete/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

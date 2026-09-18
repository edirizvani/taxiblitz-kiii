using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class DriversControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public DriversControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_ReturnsOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var response = await client.GetAsync("/Drivers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Index_ExcludesPlaceholderDriver()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Drivers.Add(new Driver { Name = "Real Driver", Email = "real@test.com" });
            db.Drivers.Add(new Driver { Name = "Doesn't matter", Email = "placeholder@test.com" });
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/Drivers");
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Doesn't matter", html);
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Drivers/Details/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Details_WithValidId_ReturnsOk()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db => db.Drivers.Add(new Driver { Name = "Driver Test" }));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        int id;
        using (var scope = factory.Services.CreateScope())
            id = scope.ServiceProvider.GetRequiredService<AppDbContext>().Drivers.First().Id;

        var response = await client.GetAsync($"/Drivers/Details/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_Get_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Drivers/Create");
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
    }

    [Fact]
    public async Task Create_Get_AdminUser_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin@drivers.com", "Administrator");
        var response = await client.GetAsync("/Drivers/Create");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Get_Admin_WithInvalidId_ReturnsNotFound()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin2@drivers.com", "Administrator");
        var response = await client.GetAsync("/Drivers/Edit/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Get_Admin_WithValidId_ReturnsOk()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db => db.Drivers.Add(new Driver { Name = "ToDelete" }));
        var client = await factory.CreateAuthenticatedClientAsync("admin3@drivers.com", "Administrator");

        int id;
        using (var scope = factory.Services.CreateScope())
            id = scope.ServiceProvider.GetRequiredService<AppDbContext>().Drivers.First().Id;

        var response = await client.GetAsync($"/Drivers/Delete/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

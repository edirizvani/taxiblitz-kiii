using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class TourStoriesControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public TourStoriesControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_ReturnsOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var response = await client.GetAsync("/TourStories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Index_WithTagFilter_ReturnsFilteredContent()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.TourPosts.AddRange(
                new TourPost { Title = "Adventure Story", Excerpt = "E", Content = "C", IsPublished = true, Tags = "adventure", CreatedDate = DateTime.Now },
                new TourPost { Title = "Culture Story",   Excerpt = "E", Content = "C", IsPublished = true, Tags = "culture",   CreatedDate = DateTime.Now });
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/TourStories?tag=adventure");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Adventure Story", html);
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFoundOrBadRequest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/TourStories/Details/99999");
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddComment_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("postId", "1"),
            new KeyValuePair<string, string>("content", "Nice post!")
        });
        var response = await client.PostAsync("/TourStories/AddComment", content);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
    }

    [Fact]
    public async Task AddComment_ContentTooLong_ReturnsBadRequest()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("commenter@test.com", "User");
        var tooLong = new string('x', 1001);
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("postId", "1"),
            new KeyValuePair<string, string>("content", tooLong)
        });

        var response = await client.PostAsync("/TourStories/AddComment", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Get_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/TourStories/Create");
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
    }

    [Fact]
    public async Task Create_Get_Admin_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin@stories.com", "Administrator");
        var response = await client.GetAsync("/TourStories/Create");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Management_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/TourStories/Management");
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
    }

    [Fact]
    public async Task Management_Admin_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin2@stories.com", "Administrator");
        var response = await client.GetAsync("/TourStories/Management");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Get_Admin_WithInvalidId_ReturnsNotFound()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("admin3@stories.com", "Administrator");
        var response = await client.GetAsync("/TourStories/Edit/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

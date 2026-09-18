using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class HomeControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public HomeControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_ReturnsOk()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        });

        var response = await client.GetAsync("/");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Index_ContainsTourContent_WhenToursSeeded()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Tours.Add(new Tour { Title = "Ohrid Lake Tour", Description = "Great tour", Price = 500, Duration = "4h" });
            db.Reviews.Add(new Review { ReviewerName = "User", Text = "Loved it!", ReviewDate = DateTime.Now, Rating = 5 });
        });
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_Post_NonAjax_WhenSuccessful_RedirectsToIndex()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("reviewerName", "John"),
            new KeyValuePair<string, string>("reviewText",   "Great experience!"),
            new KeyValuePair<string, string>("rating",       "5")
        });

        var response = await client.PostAsync("/Home/SubmitReview", content);

        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Redirect ||
                    response.StatusCode == System.Net.HttpStatusCode.Found);
    }

    [Fact]
    public async Task SubmitReview_Post_Ajax_WhenWithinLimit_ReturnsJson()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("reviewerName", "Jane"),
            new KeyValuePair<string, string>("reviewText",   "Amazing!"),
            new KeyValuePair<string, string>("rating",       "4")
        });

        var response = await client.PostAsync("/Home/SubmitReview", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("success", body, StringComparison.OrdinalIgnoreCase);
    }
}

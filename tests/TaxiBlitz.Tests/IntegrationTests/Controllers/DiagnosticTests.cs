using System.Net;
using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class DiagnosticTests
{
    [Fact]
    public async Task SeedAndQuery_SameFactory_DataVisible()
    {
        var factory = new WebAppFactory();
        var review = new Review { ReviewerName = "Diag", Text = "T", ReviewDate = DateTime.Now };
        await factory.SeedAsync(db => db.Reviews.Add(review));

        int id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = db.Reviews.Count();
            Assert.True(count > 0, $"Expected reviews in DB but found {count}");
            id = db.Reviews.First().Id;
        }

        var client = await factory.CreateAuthenticatedClientAsync("diag@admin.com", "Administrator");
        var response = await client.GetAsync($"/Reviews/Details/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

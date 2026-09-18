using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

// The health endpoints back the Docker HEALTHCHECK and the Kubernetes probes.
public class HealthCheckTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public HealthCheckTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_ReturnsHealthy(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}

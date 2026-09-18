using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Infrastructure.Weather;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Infrastructure;

public class WeatherServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public WeatherServiceTests()
    {
        _db = InMemoryDbContextFactory.Create();
    }

    public void Dispose() => _db.Dispose();

    private static IConfiguration BuildConfig(string? apiKey = "test-api-key")
    {
        var dict = new Dictionary<string, string?>();
        if (apiKey != null) dict["OpenWeatherApiKey"] = apiKey;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static HttpClient BuildHttpClient(HttpStatusCode statusCode, string json)
    {
        var handler = new MockHttpMessageHandler(statusCode, json);
        return new HttpClient(handler);
    }

    private WeatherService Build(HttpStatusCode status, string json)
        => new WeatherService(BuildHttpClient(status, json), BuildConfig(), _db);

    private WeatherService Build(HttpClient http, IConfiguration config)
        => new WeatherService(http, config, _db);

    private const string ValidWeatherJson = @"{
        ""list"": [
            { ""main"": { ""temp"": 20.5, ""temp_min"": 15.0, ""temp_max"": 25.0 },
              ""weather"": [{ ""description"": ""clear sky"", ""icon"": ""01d"" }] }
        ]
    }";

    [Fact]
    public async Task GetForecastAsync_WithNullLocation_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.OK, "{}");
        var result = await sut.GetForecastAsync(null!);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetForecastAsync_WithEmptyLocation_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.OK, "{}");
        var result = await sut.GetForecastAsync("");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetForecastAsync_WithMissingApiKey_ReturnsNull()
    {
        var sut = Build(new HttpClient(), BuildConfig(null));
        var result = await sut.GetForecastAsync("Ohrid");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetForecastAsync_OnSuccessfulApiResponse_ReturnsCacheEntry()
    {
        var sut = Build(HttpStatusCode.OK, ValidWeatherJson);

        var result = await sut.GetForecastAsync("Ohrid");

        Assert.NotNull(result);
        Assert.Equal("Ohrid", result!.Location);
        Assert.Equal("clear sky", result.ConditionText);
    }

    [Fact]
    public async Task GetForecastAsync_OnSuccessfulResponse_PersistsToCacheTable()
    {
        var sut = Build(HttpStatusCode.OK, ValidWeatherJson);

        await sut.GetForecastAsync("Ohrid");

        Assert.Equal(1, _db.WeatherCaches.Count());
    }

    [Fact]
    public async Task GetForecastAsync_WithFreshCacheEntry_ReturnsCacheWithoutHttp()
    {
        _db.WeatherCaches.Add(new WeatherCache
        {
            Location = "Ohrid", ConditionText = "cached", IconCode = "01d",
            TempCelsius = 20, TempMin = 15, TempMax = 25,
            ForecastDate = DateTime.UtcNow.Date, CachedAtUtc = DateTime.UtcNow.AddHours(-1)
        });
        await _db.SaveChangesAsync();

        var handler = new CountingHttpHandler();
        var sut = Build(new HttpClient(handler), BuildConfig());

        var result = await sut.GetForecastAsync("Ohrid");

        Assert.NotNull(result);
        Assert.Equal("cached", result!.ConditionText);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetForecastAsync_WithStaleCacheEntry_MakesNewHttpRequest()
    {
        _db.WeatherCaches.Add(new WeatherCache
        {
            Location = "Ohrid", ConditionText = "stale", IconCode = "01d",
            TempCelsius = 10, TempMin = 5, TempMax = 15,
            ForecastDate = DateTime.UtcNow.Date, CachedAtUtc = DateTime.UtcNow.AddHours(-7)
        });
        await _db.SaveChangesAsync();

        var sut = Build(HttpStatusCode.OK, ValidWeatherJson);

        var result = await sut.GetForecastAsync("Ohrid");

        Assert.NotNull(result);
        Assert.Equal("clear sky", result!.ConditionText);
    }

    [Fact]
    public async Task GetForecastAsync_OnHttpFailure_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.InternalServerError, "{}");

        var result = await sut.GetForecastAsync("Ohrid");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForecastAsync_OnNonSuccessStatusCode_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.BadGateway, "");

        var result = await sut.GetForecastAsync("Ohrid");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForecastAsync_OnEmptyResponseList_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.OK, "{\"list\":[]}");

        var result = await sut.GetForecastAsync("Ohrid");

        Assert.Null(result);
    }

    // ── Mock HTTP helpers ────────────────────────────────────────────

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _body;
        public MockHttpMessageHandler(HttpStatusCode code, string body) { _code = code; _body = body; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_code)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
    }

    private sealed class CountingHttpHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"list\":[]}", Encoding.UTF8, "application/json")
            });
        }
    }
}

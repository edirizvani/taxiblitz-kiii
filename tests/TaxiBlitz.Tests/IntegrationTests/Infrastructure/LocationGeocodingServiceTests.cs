using System.Net;
using System.Text;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Infrastructure.Maps;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Infrastructure;

public class LocationGeocodingServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public LocationGeocodingServiceTests()
    {
        _db = InMemoryDbContextFactory.Create();
    }

    public void Dispose() => _db.Dispose();

    private LocationGeocodingService Build(HttpStatusCode status, string json)
    {
        var handler = new FakeHandler(status, json);
        var http    = new HttpClient(handler);
        return new LocationGeocodingService(http, _db);
    }

    private const string ValidGeoJson = @"[{""lat"":""41.117"",""lon"":""20.801"",""display_name"":""Ohrid, North Macedonia""}]";

    [Fact]
    public async Task ResolveAsync_WithNullQuery_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.OK, ValidGeoJson);
        var result = await sut.ResolveAsync(null!);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespaceQuery_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.OK, ValidGeoJson);
        var result = await sut.ResolveAsync("   ");
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_WithCachedEntry_ReturnsCacheWithoutHttp()
    {
        _db.GeoCoordinateCaches.Add(new GeoCoordinateCache
        {
            Query = "ohrid", DisplayName = "Cached Ohrid",
            Latitude = 41.117, Longitude = 20.801,
            Provider = "Test", ResolvedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var callCount = 0;
        var handler = new CountingHandler(ref callCount);
        var sut = new LocationGeocodingService(new HttpClient(handler), _db);

        var result = await sut.ResolveAsync("Ohrid");

        Assert.NotNull(result);
        Assert.Equal("Cached Ohrid", result!.DisplayName);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task ResolveAsync_NormalizesQueryToLowercase()
    {
        _db.GeoCoordinateCaches.Add(new GeoCoordinateCache
        {
            Query = "ohrid", DisplayName = "Ohrid",
            Latitude = 41.117, Longitude = 20.801,
            Provider = "Test", ResolvedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Querying with uppercase "OHRID" should still find the lowercase cache
        var sut = Build(HttpStatusCode.OK, "[]");
        var result = await sut.ResolveAsync("OHRID");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResolveAsync_OnSuccessfulResponse_ReturnsParsedCoordinates()
    {
        var sut = Build(HttpStatusCode.OK, ValidGeoJson);

        var result = await sut.ResolveAsync("Ohrid");

        Assert.NotNull(result);
        Assert.Equal(41.117, result!.Latitude, 3);
        Assert.Equal(20.801, result.Longitude, 3);
    }

    [Fact]
    public async Task ResolveAsync_OnSuccessfulResponse_PersistsToDb()
    {
        var sut = Build(HttpStatusCode.OK, ValidGeoJson);

        await sut.ResolveAsync("Ohrid");

        Assert.Equal(1, _db.GeoCoordinateCaches.Count());
    }

    [Fact]
    public async Task ResolveAsync_OnEmptyArray_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.OK, "[]");

        var result = await sut.ResolveAsync("Nowhere");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_OnNonSuccessStatus_ReturnsNull()
    {
        var sut = Build(HttpStatusCode.NotFound, "");

        var result = await sut.ResolveAsync("Ohrid");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_OnMissingCoordinates_ReturnsNull()
    {
        var json = @"[{""display_name"":""Ohrid""}]"; // no lat/lon
        var sut = Build(HttpStatusCode.OK, json);

        var result = await sut.ResolveAsync("Ohrid");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_OnHttpException_ReturnsNull()
    {
        var handler = new ThrowingHandler();
        var sut = new LocationGeocodingService(new HttpClient(handler), _db);

        var result = await sut.ResolveAsync("Ohrid");

        Assert.Null(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _body;
        public FakeHandler(HttpStatusCode code, string body) { _code = code; _body = body; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_code) { Content = new StringContent(_body, Encoding.UTF8, "application/json") });
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly int[] _ref;
        public CountingHandler(ref int counter) { _ref = new[] { 0 }; }
        public int Count => _ref[0];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            _ref[0]++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
            => throw new HttpRequestException("Network failure");
    }
}

using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Repositories;

public class DriverRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DriverRepository _repo;

    public DriverRepositoryTests()
    {
        _db   = InMemoryDbContextFactory.Create();
        _repo = new DriverRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetActiveAsync_ExcludesPlaceholderDriver()
    {
        _db.Drivers.AddRange(
            new Driver { Name = "Real Driver" },
            new Driver { Name = "Doesn't matter" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetActiveAsync();

        Assert.Single(result);
        Assert.Equal("Real Driver", result[0].Name);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsAllNonPlaceholder()
    {
        _db.Drivers.AddRange(
            new Driver { Name = "Driver A" },
            new Driver { Name = "Driver B" },
            new Driver { Name = "Doesn't matter" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetActiveAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsMatchingDriver()
    {
        _db.Drivers.AddRange(
            new Driver { Name = "A", Email = "a@test.com" },
            new Driver { Name = "B", Email = "b@test.com" });
        await _db.SaveChangesAsync();

        var result = await _repo.GetByEmailAsync("a@test.com");

        Assert.NotNull(result);
        Assert.Equal("A", result!.Name);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenNotFound_ReturnsNull()
    {
        var result = await _repo.GetByEmailAsync("nobody@test.com");
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Roundtrip()
    {
        var driver = new Driver { Name = "Test Driver", Email = "t@test.com" };
        await _repo.AddAsync(driver);
        await _repo.SaveChangesAsync();

        var loaded = await _repo.GetByIdAsync(driver.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Test Driver", loaded!.Name);
    }
}

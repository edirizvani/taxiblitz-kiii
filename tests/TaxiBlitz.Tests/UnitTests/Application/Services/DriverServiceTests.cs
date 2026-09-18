using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Application.Services;

public class DriverServiceTests
{
    private readonly Mock<IDriverRepository>  _drivers  = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private DriverService Sut() => new(_drivers.Object, _bookings.Object);

    [Fact]
    public async Task GetFeaturedAsync_ReturnsFirstNActive()
    {
        _drivers.Setup(r => r.GetActiveAsync()).ReturnsAsync(
            Enumerable.Range(1, 5).Select(i => new Driver { Id = i, Name = $"Driver{i}" }).ToList());

        var result = await Sut().GetFeaturedAsync(3);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetFeaturedAsync_WhenFewerThanCount_ReturnsAll()
    {
        _drivers.Setup(r => r.GetActiveAsync()).ReturnsAsync(
            new List<Driver> { new Driver { Name = "A" }, new Driver { Name = "B" } });

        var result = await Sut().GetFeaturedAsync(5);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetFeaturedAsync_WhenEmpty_ReturnsEmptyList()
    {
        _drivers.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Driver>());

        var result = await Sut().GetFeaturedAsync(3);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteAsync_ReassignsBookingsBeforeDeleting()
    {
        var driver = new Driver { Id = 10, Name = "Test" };
        _drivers.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(driver);

        await Sut().DeleteAsync(10);

        _bookings.Verify(r => r.ReassignDriverAsync(10, 4), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenDriverNotFound()
    {
        _drivers.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Driver?)null);

        await Sut().DeleteAsync(99);

        _drivers.Verify(r => r.DeleteAsync(It.IsAny<Driver>()), Times.Never);
        _drivers.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CallsSaveChanges_WhenDriverFound()
    {
        var driver = new Driver { Id = 5, Name = "Test" };
        _drivers.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(driver);

        await Sut().DeleteAsync(5);

        _drivers.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAsync_CallsAddAndSave()
    {
        var driver = new Driver { Name = "New Driver" };

        await Sut().AddAsync(driver);

        _drivers.Verify(r => r.AddAsync(driver), Times.Once);
        _drivers.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsUpdateAndSave()
    {
        var driver = new Driver { Id = 3, Name = "Updated" };

        await Sut().UpdateAsync(driver);

        _drivers.Verify(r => r.UpdateAsync(driver), Times.Once);
        _drivers.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_DelegatesToRepository()
    {
        var email = "driver@test.com";
        _drivers.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((Driver?)null);

        await Sut().GetByEmailAsync(email);

        _drivers.Verify(r => r.GetByEmailAsync(email), Times.Once);
    }
}

using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Domain;

public class DriverEntityTests
{
    [Fact]
    public void Rating_AcceptsNull()
    {
        var driver = new Driver { Rating = null };
        Assert.Null(driver.Rating);
    }

    [Fact]
    public void Rating_AcceptsZero()
    {
        var driver = new Driver { Rating = 0.0 };
        Assert.Equal(0.0, driver.Rating);
    }

    [Fact]
    public void Rating_AcceptsFive()
    {
        var driver = new Driver { Rating = 5.0 };
        Assert.Equal(5.0, driver.Rating);
    }

    [Fact]
    public void PictureUrls_DefaultsEmpty()
    {
        var driver = new Driver();
        Assert.NotNull(driver.PictureUrls);
        Assert.Empty(driver.PictureUrls);
    }
}

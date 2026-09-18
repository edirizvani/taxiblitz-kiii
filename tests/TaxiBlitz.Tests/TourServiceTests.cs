using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests
{
    public class TourServiceTests
    {
        private readonly Mock<ITourRepository>    _mockRepo;
        private readonly Mock<IBookingRepository> _mockBookings;
        private readonly TourService              _service;

        public TourServiceTests()
        {
            _mockRepo     = new Mock<ITourRepository>();
            _mockBookings = new Mock<IBookingRepository>();
            _service      = new TourService(_mockRepo.Object, _mockBookings.Object);
        }

        [Fact]
        public async Task SearchAsync_ReturnsPagedResults()
        {
            var tours = new List<Tour>
            {
                new() { Id = 1, Title = "Lake Tour", Description = "Scenic", StartingPoint = "Ohrid", EndingPoint = "Struga", RouteStopsText = "" },
                new() { Id = 2, Title = "Mountain Hike", Description = "Epic", StartingPoint = "Ohrid", EndingPoint = "Galichica", RouteStopsText = "" }
            };
            _mockRepo.Setup(r => r.SearchAsync(null, null, 0, 9)).ReturnsAsync(tours);
            _mockRepo.Setup(r => r.CountAsync(null)).ReturnsAsync(2);

            var (result, total) = await _service.SearchAsync(null, null, 1, 9);

            Assert.Equal(2, total);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SearchAsync_CalculatesCorrectSkip()
        {
            _mockRepo.Setup(r => r.SearchAsync("lake", "price-asc", 9, 9)).ReturnsAsync(new List<Tour>());
            _mockRepo.Setup(r => r.CountAsync("lake")).ReturnsAsync(0);

            await _service.SearchAsync("lake", "price-asc", 2, 9);

            _mockRepo.Verify(r => r.SearchAsync("lake", "price-asc", 9, 9), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Tour?)null);

            var result = await _service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_CallsRepositoryAddAndSave()
        {
            var tour = new Tour { Title = "New Tour", Description = "Desc", StartingPoint = "A", EndingPoint = "B", RouteStopsText = "" };
            _mockRepo.Setup(r => r.AddAsync(tour)).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.AddAsync(tour);

            _mockRepo.Verify(r => r.AddAsync(tour), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_DoesNothing_WhenTourNotFound()
        {
            _mockBookings.Setup(r => r.ReassignTourAsync(42, 15)).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Tour?)null);

            await _service.DeleteAsync(42);

            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Tour>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task GetReviewStatsAsync_ReturnsCorrectValues()
        {
            _mockRepo.Setup(r => r.GetReviewStatsAsync()).ReturnsAsync((5, (double?)4.2));

            var (count, avg) = await _service.GetReviewStatsAsync();

            Assert.Equal(5, count);
            Assert.Equal(4.2, avg);
        }
    }
}

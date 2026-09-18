using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _mockRepo;
        private readonly Mock<IEmailService>      _mockEmail;
        private readonly BookingService            _service;

        public BookingServiceTests()
        {
            _mockRepo  = new Mock<IBookingRepository>();
            _mockEmail = new Mock<IEmailService>();
            _service   = new BookingService(_mockRepo.Object, _mockEmail.Object);
        }

        [Fact]
        public async Task AddAsync_SetsPendingStatus_AndSendsAlert()
        {
            var booking = new BookingTour { Id = 1, NameOfBookMaker = "Alice", PhoneNumber = "123", TourId = 1 };
            _mockRepo.Setup(r => r.AddAsync(booking)).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.GetByIdWithIncludesAsync(1)).ReturnsAsync(booking);

            await _service.AddAsync(booking, "admin@test.com");

            Assert.Equal("Pending", booking.Status);
            _mockEmail.Verify(e => e.SendAdminNewBookingAlert(booking, "admin@test.com"), Times.Once);
        }

        [Fact]
        public async Task ApproveAsync_SetsApprovedStatus_ReturnsConfirmationText()
        {
            var booking = new BookingTour { Id = 5, NameOfBookMaker = "Bob", PhoneNumber = "999", TourId = 2 };
            _mockRepo.Setup(r => r.GetByIdWithIncludesAsync(5)).ReturnsAsync(booking);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockEmail.Setup(e => e.BuildConfirmationText(booking)).Returns("Confirmed!");
            _mockEmail.Setup(e => e.SendBookingConfirmation(booking)).Returns("");

            var (text, error) = await _service.ApproveAsync(5);

            Assert.Equal("Approved", booking.Status);
            Assert.Equal("Confirmed!", text);
            Assert.Null(error);
        }

        [Fact]
        public async Task ApproveAsync_ReturnsError_WhenEmailFails()
        {
            var booking = new BookingTour { Id = 3, NameOfBookMaker = "Carol", PhoneNumber = "111", TourId = 1 };
            _mockRepo.Setup(r => r.GetByIdWithIncludesAsync(3)).ReturnsAsync(booking);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockEmail.Setup(e => e.BuildConfirmationText(booking)).Returns("text");
            _mockEmail.Setup(e => e.SendBookingConfirmation(booking)).Returns("SMTP error");

            var (_, error) = await _service.ApproveAsync(3);

            Assert.Equal("SMTP error", error);
        }

        [Fact]
        public async Task CancelAsync_SetsCanceledStatus()
        {
            var booking = new BookingTour { Id = 7, NameOfBookMaker = "Dave", PhoneNumber = "222", TourId = 1, Status = "Pending" };
            _mockRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(booking);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.CancelAsync(7);

            Assert.Equal("Canceled", booking.Status);
        }

        [Fact]
        public async Task DeleteAsync_DoesNothing_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BookingTour?)null);

            await _service.DeleteAsync(99);

            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<BookingTour>()), Times.Never);
        }
    }
}

using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository   _drivers;
        private readonly IBookingRepository  _bookings;
        public DriverService(IDriverRepository drivers, IBookingRepository bookings)
        {
            _drivers  = drivers;
            _bookings = bookings;
        }

        public Task<List<Driver>> GetActiveAsync() => _drivers.GetActiveAsync();

        public async Task<List<Driver>> GetFeaturedAsync(int count)
        {
            var active = await _drivers.GetActiveAsync();
            return active.Take(count).ToList();
        }

        public Task<Driver?> GetByIdAsync(int id) => _drivers.GetByIdAsync(id);

        public Task<Driver?> GetByEmailAsync(string email) => _drivers.GetByEmailAsync(email);

        public async Task AddAsync(Driver driver)
        {
            await _drivers.AddAsync(driver);
            await _drivers.SaveChangesAsync();
        }

        public async Task UpdateAsync(Driver driver)
        {
            await _drivers.UpdateAsync(driver);
            await _drivers.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _bookings.ReassignDriverAsync(id, 4);
            var driver = await _drivers.GetByIdAsync(id);
            if (driver == null) return;
            await _drivers.DeleteAsync(driver);
            await _drivers.SaveChangesAsync();
        }
    }
}

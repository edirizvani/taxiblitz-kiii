using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Persistence.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _db;
        public BookingRepository(AppDbContext db) => _db = db;

        public async Task<BookingTour?> GetByIdAsync(int id) =>
            await _db.Bookings.FindAsync(id);

        public async Task<IReadOnlyList<BookingTour>> GetAllAsync() =>
            await _db.Bookings.ToListAsync();

        public async Task AddAsync(BookingTour entity) =>
            await _db.Bookings.AddAsync(entity);

        public Task UpdateAsync(BookingTour entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(BookingTour entity)
        {
            _db.Bookings.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() =>
            await _db.SaveChangesAsync();

        public async Task<List<BookingTour>> GetAllWithIncludesAsync() =>
            await _db.Bookings.Include(b => b.Driver).Include(b => b.Tour).ToListAsync();

        public async Task<BookingTour?> GetByIdWithIncludesAsync(int id) =>
            await _db.Bookings.Include(b => b.Tour).Include(b => b.Driver).FirstOrDefaultAsync(b => b.Id == id);

        public async Task<List<BookingTour>> GetByUserEmailAsync(string email) =>
            await _db.Bookings.Include(b => b.Tour).Include(b => b.Driver).Where(b => b.CustomerEmail == email).ToListAsync();

        public async Task<List<BookingTour>> GetByDriverIdAsync(int driverId) =>
            await _db.Bookings.Where(b => b.DriverId == driverId).ToListAsync();

        public async Task<List<BookingTour>> GetApprovedAsync() =>
            await _db.Bookings.Include(b => b.Tour).Include(b => b.Driver).Where(b => b.Status == "Approved").ToListAsync();

        public async Task ReassignTourAsync(int tourId, int fallbackTourId)
        {
            var affected = await _db.Bookings.Where(b => b.TourId == tourId).ToListAsync();
            foreach (var b in affected)
            {
                b.TourId = fallbackTourId;
                b.Status = "Pending";
            }
            await _db.SaveChangesAsync();
        }

        public async Task ReassignDriverAsync(int driverId, int fallbackDriverId)
        {
            var affected = await _db.Bookings.Where(b => b.DriverId == driverId).ToListAsync();
            foreach (var b in affected)
            {
                b.DriverId = fallbackDriverId;
                b.Status   = "Pending";
            }
            await _db.SaveChangesAsync();
        }
    }
}

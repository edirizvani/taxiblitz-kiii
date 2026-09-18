using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Persistence.Repositories
{
    public class DriverRepository : IDriverRepository
    {
        private readonly AppDbContext _db;
        public DriverRepository(AppDbContext db) => _db = db;

        public async Task<Driver?> GetByIdAsync(int id) =>
            await _db.Drivers.FindAsync(id);

        public async Task<IReadOnlyList<Driver>> GetAllAsync() =>
            await _db.Drivers.ToListAsync();

        public async Task AddAsync(Driver entity) =>
            await _db.Drivers.AddAsync(entity);

        public Task UpdateAsync(Driver entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Driver entity)
        {
            _db.Drivers.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() =>
            await _db.SaveChangesAsync();

        public async Task<List<Driver>> GetActiveAsync() =>
            await _db.Drivers.Where(d => d.Name != "Doesn't matter").ToListAsync();

        public async Task<Driver?> GetByEmailAsync(string email) =>
            await _db.Drivers.FirstOrDefaultAsync(d => d.Email == email);
    }
}

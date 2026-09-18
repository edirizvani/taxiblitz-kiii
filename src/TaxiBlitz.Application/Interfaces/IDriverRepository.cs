using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Interfaces
{
    public interface IDriverRepository : IRepository<Driver>
    {
        Task<List<Driver>> GetActiveAsync();
        Task<Driver?> GetByEmailAsync(string email);
    }
}

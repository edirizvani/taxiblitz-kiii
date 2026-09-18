using System.Collections.Generic;
using System.Threading.Tasks;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Interfaces
{
    public interface IReferralRepository
    {
        Task<ReferralCode?> GetByCodeAsync(string code);
        Task<ReferralCode?> GetByOwnerIdAsync(string ownerId);
        Task<ReferralCode> CreateAsync(ReferralCode code);
        Task IncrementUsageAsync(int codeId);
        Task AddUsageAsync(ReferralUsage usage);
        Task<bool> HasUserUsedCodeAsync(int codeId, string userId);
        Task SaveChangesAsync();

        Task<List<ReferralCode>> GetAllByOwnerIdAsync(string ownerId);
        Task<ReferralCode> GenerateNewAsync(string ownerId);
        Task<int> MarkCommissionPaidAsync(string ownerId);
    }
}

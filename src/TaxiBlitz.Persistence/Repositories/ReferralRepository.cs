using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Persistence.Repositories
{
    public class ReferralRepository : IReferralRepository
    {
        private readonly AppDbContext _db;
        public ReferralRepository(AppDbContext db) => _db = db;

        public async Task<ReferralCode?> GetByCodeAsync(string code)
            => await _db.ReferralCodes.FirstOrDefaultAsync(r => r.Code == code);

        public async Task<ReferralCode?> GetByOwnerIdAsync(string ownerId)
            => await _db.ReferralCodes.FirstOrDefaultAsync(r => r.OwnerId == ownerId);

        public async Task<ReferralCode> CreateAsync(ReferralCode code)
        {
            _db.ReferralCodes.Add(code);
            await _db.SaveChangesAsync();
            return code;
        }

        public async Task IncrementUsageAsync(int codeId)
        {
            var code = await _db.ReferralCodes.FindAsync(codeId);
            if (code != null)
            {
                code.UsageCount++;
                await _db.SaveChangesAsync();
            }
        }

        public async Task AddUsageAsync(ReferralUsage usage)
            => await _db.ReferralUsages.AddAsync(usage);

        public async Task<bool> HasUserUsedCodeAsync(int codeId, string userId)
            => await _db.ReferralUsages.AnyAsync(ru => ru.ReferralCodeId == codeId && ru.UsedByUserId == userId);

        public Task SaveChangesAsync() => _db.SaveChangesAsync();

        public async Task<List<ReferralCode>> GetAllByOwnerIdAsync(string ownerId)
            => await _db.ReferralCodes
                .Where(r => r.OwnerId == ownerId)
                .Include(r => r.Usages)
                    .ThenInclude(u => u.UsedByUser)
                .Include(r => r.Usages)
                    .ThenInclude(u => u.Booking)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<ReferralCode> GenerateNewAsync(string ownerId)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var candidate = "RC" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
                var code = new ReferralCode
                {
                    Code      = candidate,
                    OwnerId   = ownerId,
                    MaxUses   = 1,
                    CreatedAt = DateTime.UtcNow
                };
                try
                {
                    _db.ReferralCodes.Add(code);
                    await _db.SaveChangesAsync();
                    return code;
                }
                catch (DbUpdateException)
                {
                    _db.Entry(code).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
            throw new InvalidOperationException("Could not generate a unique referral code after 3 attempts.");
        }

        public async Task<int> MarkCommissionPaidAsync(string ownerId)
        {
            var now = DateTime.UtcNow;
            var usages = await _db.ReferralUsages
                .Include(u => u.ReferralCode)
                .Include(u => u.Booking)
                .Where(u => u.ReferralCode.OwnerId == ownerId
                         && u.Booking != null
                         && u.Booking.Status == "Approved"
                         && u.CommissionPaidAt == null)
                .ToListAsync();
            foreach (var usage in usages)
                usage.CommissionPaidAt = now;
            await _db.SaveChangesAsync();
            return usages.Count;
        }
    }
}

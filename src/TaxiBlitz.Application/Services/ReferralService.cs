using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Application.ViewModels;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Application.Services
{
    public class ReferralService : IReferralService
    {
        private readonly IReferralRepository _repo;

        public ReferralService(IReferralRepository repo) => _repo = repo;

        public async Task<ReferralCode> GetOrCreateCodeAsync(string userId)
        {
            var existing = await _repo.GetByOwnerIdAsync(userId);
            if (existing != null) return existing;

            var code = new ReferralCode
            {
                Code      = GenerateCode(userId),
                OwnerId   = userId,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.CreateAsync(code);
        }

        public async Task<(bool valid, decimal discountPercent, string message, int? codeId)> ValidateCodeAsync(string code, string userId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (false, 0, "Please enter a referral code.", null);

            var referral = await _repo.GetByCodeAsync(code.Trim().ToUpperInvariant());
            if (referral == null)
                return (false, 0, "Referral code not found.", null);
            if (!referral.IsActive)
                return (false, 0, "This referral code is no longer active.", null);
            if (referral.UsageCount >= referral.MaxUses)
                return (false, 0, "This referral code has reached its usage limit.", null);
            if (referral.OwnerId == userId)
                return (false, 0, "You cannot use your own referral code.", null);
            
            // Check if user has already used this code
            var hasUsed = await _repo.HasUserUsedCodeAsync(referral.Id, userId);
            if (hasUsed)
                return (false, 0, "You have already used this referral code.", null);

            return (true, referral.DiscountPercent, $"Code applied! {referral.DiscountPercent}% discount.", referral.Id);
        }

        public async Task RecordUsageAsync(string code, int bookingId, string userId, decimal discountAmount)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var referral = await _repo.GetByCodeAsync(code.Trim().ToUpperInvariant());
            if (referral == null) return;

            await _repo.AddUsageAsync(new ReferralUsage
            {
                ReferralCodeId = referral.Id,
                UsedByUserId   = userId,
                BookingId      = bookingId,
                DiscountAmount = discountAmount,
                UsedAt         = DateTime.UtcNow
            });
            await _repo.IncrementUsageAsync(referral.Id);
            await _repo.SaveChangesAsync();
        }

        public Task<ReferralCode> GenerateNewCodeAsync(string userId) =>
            _repo.GenerateNewAsync(userId);

        public async Task<ReceptionistDashboardViewModel> GetReceptionistDashboardAsync(string userId)
        {
            var codes = await _repo.GetAllByOwnerIdAsync(userId);

            var rows = new List<ReceptionistCodeRowViewModel>();
            decimal totalEarned = 0, totalPaid = 0;

            foreach (var rc in codes)
            {
                if (rc.Usages == null || rc.Usages.Count == 0)
                {
                    rows.Add(new ReceptionistCodeRowViewModel
                    {
                        CodeId        = rc.Id,
                        Code          = rc.Code,
                        IsActive      = rc.IsActive,
                        CreatedAt     = rc.CreatedAt,
                        BookingStatus = "Not Used"
                    });
                }
                else
                {
                    foreach (var usage in rc.Usages)
                    {
                        var bookingStatus = usage.Booking?.Status ?? "Not Used";
                        decimal? commission = bookingStatus == "Approved" ? usage.DiscountAmount : null;
                        var paid = usage.CommissionPaidAt != null;

                        if (commission.HasValue)
                        {
                            totalEarned += commission.Value;
                            if (paid) totalPaid += commission.Value;
                        }

                        rows.Add(new ReceptionistCodeRowViewModel
                        {
                            CodeId           = rc.Id,
                            Code             = rc.Code,
                            IsActive         = rc.IsActive,
                            CreatedAt        = rc.CreatedAt,
                            UsedByEmail      = usage.UsedByUser?.Email,
                            BookingDate      = usage.Booking?.BookingDateTime,
                            BookingStatus    = bookingStatus,
                            CommissionAmount = commission,
                            CommissionPaid   = paid,
                            CommissionPaidAt = usage.CommissionPaidAt
                        });
                    }
                }
            }

            return new ReceptionistDashboardViewModel
            {
                TotalEarned  = totalEarned,
                TotalPaid    = totalPaid,
                TotalPending = totalEarned - totalPaid,
                Codes        = rows
            };
        }

        public async Task<IList<ReceptionistSummaryViewModel>> GetAllReceptionistSummariesAsync(IList<ApplicationUser> receptionists)
        {
            var summaries = new List<ReceptionistSummaryViewModel>();
            foreach (var user in receptionists)
            {
                var codes = await _repo.GetAllByOwnerIdAsync(user.Id);
                var allUsages = codes.SelectMany(c => c.Usages ?? new List<ReferralUsage>()).ToList();
                var approvedUsages = allUsages.Where(u => u.Booking?.Status == "Approved").ToList();
                var totalEarned = approvedUsages.Sum(u => u.DiscountAmount);
                var pending = approvedUsages.Where(u => u.CommissionPaidAt == null).Sum(u => u.DiscountAmount);

                summaries.Add(new ReceptionistSummaryViewModel
                {
                    UserId              = user.Id,
                    FullName            = $"{user.FirstName} {user.LastName}".Trim(),
                    Email               = user.Email ?? string.Empty,
                    TotalCodesGenerated = codes.Count,
                    TotalBookings       = allUsages.Count(u => u.BookingId != null),
                    ApprovedBookings    = approvedUsages.Count,
                    TotalEarned         = totalEarned,
                    PendingCommission   = pending
                });
            }
            return summaries;
        }

        public async Task<int> PayCommissionAsync(string receptionistUserId) =>
            await _repo.MarkCommissionPaidAsync(receptionistUserId);

        private static string GenerateCode(string userId)
        {
            var suffix = Math.Abs(userId.GetHashCode() ^ DateTime.UtcNow.Ticks.GetHashCode()) % 10000;
            return $"TB{suffix:D4}";
        }
    }
}

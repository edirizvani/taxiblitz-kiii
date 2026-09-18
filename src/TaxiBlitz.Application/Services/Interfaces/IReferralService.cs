using System.Collections.Generic;
using System.Threading.Tasks;
using TaxiBlitz.Application.ViewModels;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Application.Services.Interfaces
{
    public interface IReferralService
    {
        Task<ReferralCode> GetOrCreateCodeAsync(string userId);
        Task<(bool valid, decimal discountPercent, string message, int? codeId)> ValidateCodeAsync(string code, string userId);
        Task RecordUsageAsync(string code, int bookingId, string userId, decimal discountAmount);

        Task<ReferralCode> GenerateNewCodeAsync(string userId);
        Task<ReceptionistDashboardViewModel> GetReceptionistDashboardAsync(string userId);
        Task<IList<ReceptionistSummaryViewModel>> GetAllReceptionistSummariesAsync(IList<ApplicationUser> receptionists);
        Task<int> PayCommissionAsync(string receptionistUserId);
    }
}

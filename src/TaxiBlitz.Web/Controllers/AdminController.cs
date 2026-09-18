using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Application.ViewModels;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Web.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly IReferralService             _referralService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(IReferralService referralService,
            UserManager<ApplicationUser> userManager)
        {
            _referralService = referralService;
            _userManager     = userManager;
        }

        public async Task<IActionResult> Receptionists()
        {
            var receptionists = await _userManager.GetUsersInRoleAsync("Receptionist");
            var summaries = await _referralService.GetAllReceptionistSummariesAsync(receptionists.ToList());
            return View(summaries);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayCommission(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest();
            var count = await _referralService.PayCommissionAsync(userId);
            TempData["Success"] = $"Marked {count} commission(s) as paid.";
            return RedirectToAction("Receptionists");
        }

        public async Task<IActionResult> ReceptionistDetail(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            var dashboard = await _referralService.GetReceptionistDashboardAsync(userId);
            var vm = new AdminReceptionistDetailViewModel
            {
                ReceptionistEmail = user.Email ?? string.Empty,
                ReceptionistName  = $"{user.FirstName} {user.LastName}".Trim(),
                Dashboard         = dashboard
            };
            return View(vm);
        }

        public async Task<IActionResult> AdminCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var vm = await _referralService.GetReceptionistDashboardAsync(user.Id);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            await _referralService.GenerateNewCodeAsync(user.Id);
            TempData["Success"] = "New referral code generated successfully.";
            return RedirectToAction("AdminCodes");
        }
    }
}

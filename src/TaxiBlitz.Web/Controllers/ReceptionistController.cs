using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Web.Controllers
{
    [Authorize(Roles = "Receptionist,Administrator")]
    public class ReceptionistController : Controller
    {
        private readonly IReferralService             _referralService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReceptionistController(IReferralService referralService,
            UserManager<ApplicationUser> userManager)
        {
            _referralService = referralService;
            _userManager     = userManager;
        }

        public async Task<IActionResult> Dashboard()
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
            return RedirectToAction("Dashboard");
        }
    }
}

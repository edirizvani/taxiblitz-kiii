using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Web.Controllers
{
    public class DriversController : Controller
    {
        private readonly IDriverService _driverService;

        public DriversController(IDriverService driverService)
        {
            _driverService = driverService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title           = "Our Local Ohrid Drivers — Meet the Taxi Blitz Team";
            ViewBag.MetaDescription = "Meet the Taxi Blitz Ohrid team — experienced local drivers with 25+ years on Lake Ohrid. Professional, multilingual, and available 24/7 for tours and transfers.";
            ViewBag.CanonicalUrl    = Url.Action("Index", "Drivers", null, Request.Scheme);
            return View(await _driverService.GetActiveAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();
            var driver = await _driverService.GetByIdAsync(id.Value);
            if (driver == null) return NotFound();
            return View(driver);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Bio,PhotoProfileUrl,Rating,Languages,VehicleType,CoverageAreas,ExperienceYears,TripsCompleted,Specialties")] Driver driver)
        {
            NormalizeRating(driver);
            if (ModelState.IsValid)
            {
                await _driverService.AddAsync(driver);
                return RedirectToAction("Index");
            }
            return View(driver);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();
            var driver = await _driverService.GetByIdAsync(id.Value);
            if (driver == null) return NotFound();
            return View(driver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit([Bind("Id,Name,Email,Bio,PhotoProfileUrl,Rating,Languages,VehicleType,CoverageAreas,ExperienceYears,TripsCompleted,Specialties")] Driver driver)
        {
            NormalizeRating(driver);
            if (ModelState.IsValid)
            {
                await _driverService.UpdateAsync(driver);
                return RedirectToAction("Index");
            }
            return View(driver);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();
            var driver = await _driverService.GetByIdAsync(id.Value);
            if (driver == null) return NotFound();
            return View(driver);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _driverService.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        // Handles European decimal notation (4,94 → 4.94) which the default model binder rejects.
        private void NormalizeRating(Driver driver)
        {
            if (!Request.Form.TryGetValue("Rating", out var raw)) return;
            var normalized = raw.ToString().Replace(',', '.');
            if (!double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return;
            driver.Rating = value;
            ModelState.Remove("Rating");
        }
    }
}

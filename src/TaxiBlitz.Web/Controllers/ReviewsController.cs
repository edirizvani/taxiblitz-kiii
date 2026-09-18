using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Web.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index() =>
            View(await _reviewService.GetAllAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();
            var review = await _reviewService.GetByIdAsync(id.Value);
            if (review == null) return NotFound();
            return View(review);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ReviewerName,Text,ReviewDate,ImageUrl")] Review review)
        {
            if (ModelState.IsValid)
            {
                await _reviewService.AddAsync(review);
                return RedirectToAction("Index");
            }
            return View(review);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();
            var review = await _reviewService.GetByIdAsync(id.Value);
            if (review == null) return NotFound();
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,ReviewerName,Text,ReviewDate,ImageUrl")] Review review)
        {
            if (ModelState.IsValid)
            {
                await _reviewService.UpdateAsync(review);
                return RedirectToAction("Index");
            }
            return View(review);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();
            var review = await _reviewService.GetByIdAsync(id.Value);
            if (review == null) return NotFound();
            return View(review);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _reviewService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}

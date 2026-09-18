using System;
using System.IO;
using System.Threading.Tasks;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Web.Controllers
{
    public class TourStoriesController : Controller
    {
        private readonly ITourStoryService             _storyService;
        private readonly ITourService                  _tourService;
        private readonly UserManager<ApplicationUser>  _userManager;
        private readonly IWebHostEnvironment           _env;
        private readonly ILogger<TourStoriesController> _logger;

        public TourStoriesController(ITourStoryService storyService, ITourService tourService,
            UserManager<ApplicationUser> userManager, IWebHostEnvironment env,
            ILogger<TourStoriesController> logger)
        {
            _storyService = storyService;
            _tourService  = tourService;
            _userManager  = userManager;
            _env          = env;
            _logger       = logger;
        }

        public async Task<IActionResult> Index(int? page, string tag = null)
        {
            int pageNumber = page ?? 1;
            var (allPosts, _) = await _storyService.GetPublishedPagedAsync(1, 9999);

            // Collect distinct tags for filter pills
            var allTags = allPosts
                .Where(p => !string.IsNullOrWhiteSpace(p.Tags))
                .SelectMany(p => p.Tags.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // Filter by tag if provided
            IEnumerable<TourPost> filtered = allPosts;
            if (!string.IsNullOrWhiteSpace(tag))
                filtered = allPosts.Where(p => p.Tags != null && p.Tags.Contains(tag, System.StringComparison.OrdinalIgnoreCase));

            int pageSize   = 9;
            int totalCount = filtered.Count();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var posts      = filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalPages      = totalPages;
            ViewBag.CurrentPage     = pageNumber;
            ViewBag.ActiveTag       = tag ?? "";
            ViewBag.AllTags         = allTags;
            ViewBag.Title           = "Tour Stories — Travel Tales from Ohrid & Albania";
            ViewBag.MetaDescription = "Real stories from travellers who explored Lake Ohrid, Albania, and North Macedonia with Taxi Blitz Ohrid. Read about day trips to Tirana, Galicica, Saint Naum and more.";
            ViewBag.CanonicalUrl    = Url.Action("Index", "TourStories", null, Request.Scheme);
            return View("~/Views/Home/TourStoriesIndex.cshtml", posts);
        }

        public async Task<IActionResult> Details(int? id, string slug)
        {
            if (id == null) return BadRequest();

            var post = await _storyService.GetByIdWithDetailsAsync(id.Value);
            if (post == null) return NotFound();

            var sanitizer = new HtmlSanitizer();
            ViewBag.SafeContent  = sanitizer.Sanitize(post.Content ?? string.Empty);
            ViewBag.RelatedPosts = await _storyService.GetRelatedAsync(post, 3);
            return View("~/Views/Home/TourStoriesDetails.cshtml", post);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Tours = new SelectList(await _tourService.GetActiveAsync(), "Id", "Title");
            return View("~/Views/Home/TourStoriesCreateEdit.cshtml", new TourPost());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(TourPost tourPost, string[] imageUrls, string[] imageCaptions)
        {
            if (ModelState.IsValid)
            {
                await _storyService.AddAsync(tourPost, _userManager.GetUserId(User), imageUrls, imageCaptions);
                return RedirectToAction("Management", "TourStories");
            }

            ViewBag.Tours = new SelectList(await _tourService.GetActiveAsync(), "Id", "Title", tourPost.RelatedTourId);
            return View("~/Views/Home/TourStoriesCreateEdit.cshtml", tourPost);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, error = "No file uploaded" });

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext     = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Array.Exists(allowed, e => e == ext))
                return Json(new { success = false, error = "Invalid file type" });

            if (file.Length > 5 * 1024 * 1024)
                return Json(new { success = false, error = "File too large" });

            try
            {
                var uploads  = Path.Combine(_env.WebRootPath, "gallery");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid().ToString("N") + ext;
                var path     = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                    await file.CopyToAsync(stream);

                return Json(new { success = true, url = Url.Content("~/gallery/" + fileName) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading story image");
                return Json(new { success = false, error = "Upload failed. Please try again." });
            }
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();
            var post = await _storyService.GetByIdForEditAsync(id.Value);
            if (post == null) return NotFound();
            ViewBag.Tours = new SelectList(await _tourService.GetActiveAsync(), "Id", "Title", post.RelatedTourId);
            return View("~/Views/Home/TourStoriesCreateEdit.cshtml", post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(TourPost tourPost, string[] imageUrls, string[] imageCaptions)
        {
            if (ModelState.IsValid)
            {
                await _storyService.UpdateAsync(tourPost, imageUrls, imageCaptions);
                return RedirectToAction("Management", "TourStories");
            }

            ViewBag.Tours = new SelectList(await _tourService.GetActiveAsync(), "Id", "Title", tourPost.RelatedTourId);
            return View("~/Views/Home/TourStoriesCreateEdit.cshtml", tourPost);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();
            var post = await _storyService.GetByIdForEditAsync(id.Value);
            if (post == null) return NotFound();
            return View("~/Views/Home/TourStoriesDelete.cshtml", post);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _storyService.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
                return BadRequest();

            await _storyService.AddCommentAsync(postId, content, _userManager.GetUserId(User));
            return RedirectToAction("Details", new { id = postId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId, int postId)
        {
            await _storyService.DeleteCommentAsync(commentId, _userManager.GetUserId(User), User.IsInRole("Administrator"));
            return RedirectToAction("Details", new { id = postId });
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Management() =>
            View("~/Views/Home/TourStoriesManagement.cshtml", await _storyService.GetAllForManagementAsync());
    }
}

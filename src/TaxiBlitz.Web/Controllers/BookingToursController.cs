using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;
using TaxiBlitz.Infrastructure.Receipts;

namespace TaxiBlitz.Web.Controllers
{
    public class BookingToursController : Controller
    {
        private readonly IBookingService              _bookingService;
        private readonly ITourService                 _tourService;
        private readonly IDriverService               _driverService;
        private readonly IConfiguration               _config;
        private readonly IReceiptService              _receiptService;
        private readonly IReferralService             _referralService;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingToursController(IBookingService bookingService, ITourService tourService,
            IDriverService driverService, IConfiguration config, IReceiptService receiptService,
            IReferralService referralService, UserManager<ApplicationUser> userManager)
        {
            _bookingService  = bookingService;
            _tourService     = tourService;
            _driverService   = driverService;
            _config          = config;
            _receiptService  = receiptService;
            _referralService = referralService;
            _userManager     = userManager;
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Index() =>
            View(await _bookingService.GetAllWithIncludesAsync());

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AdministratorIndex() =>
            View(await _bookingService.GetAllWithIncludesAsync());

        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();
            var booking = await _bookingService.GetByIdAsync(id.Value);
            if (booking == null) return NotFound();
            if (booking.CustomerEmail != User.Identity.Name && !User.IsInRole("Administrator"))
                return Forbid();
            return View(booking);
        }

        [Authorize]
        public async Task<IActionResult> Create(int? tourId = null, string bookingName = null, int? driverId = null, string phoneNumber = null, int? numberOfPeople = null)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                phoneNumber = currentUser?.PhoneNumber;
            }

            var model = new BookingTour
            {
                TourId          = tourId ?? 0,
                DriverId        = driverId,
                NameOfBookMaker = string.IsNullOrWhiteSpace(bookingName) ? User.Identity.Name : bookingName,
                PhoneNumber     = phoneNumber,
                NumberOfPeople  = numberOfPeople ?? 1,
                BookingDateTime = System.DateTime.Now.AddDays(1).AddHours(1)
            };

            var drivers      = await _driverService.GetActiveAsync();
            var activeTours  = await _tourService.GetActiveAsync();
            ViewBag.DriverId     = new SelectList(drivers, "Id", "Name", model.DriverId);
            ViewBag.TourId       = new SelectList(activeTours, "Id", "Title", model.TourId);
            ViewBag.TourPrices   = activeTours.ToDictionary(t => t.Id.ToString(), t => t.Price);
            ViewBag.SelectedTour = tourId.HasValue ? (await _tourService.GetByIdAsync(tourId.Value)) : null;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(
            [Bind("Id,NameOfBookMaker,TourId,DriverId,PhoneNumber,NumberOfPeople,BookingDateTime")] BookingTour booking,
            string referralCode = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            booking.CustomerEmail = User.Identity.Name;
            ModelState.Remove("CustomerEmail");
            var minAllowedTime    = System.DateTime.Now.AddHours(1);

            if (booking.BookingDateTime < minAllowedTime)
                ModelState.AddModelError("BookingDateTime", "Bookings must be at least 1 hour in the future.");

            if (ModelState.IsValid)
            {
                var selectedTour   = await _tourService.GetByIdAsync(booking.TourId);
                var selectedDriver = booking.DriverId.HasValue
                    ? await _driverService.GetByIdAsync(booking.DriverId.Value)
                    : null;

                if (selectedTour == null)
                    ModelState.AddModelError("TourId", "Please select a valid tour.");
                if (booking.DriverId.HasValue && selectedDriver == null)
                    ModelState.AddModelError("DriverId", "Please select a valid driver.");

                if (!ModelState.IsValid)
                {
                    var drivers2     = await _driverService.GetActiveAsync();
                    var activeTours2 = await _tourService.GetActiveAsync();
                    ViewBag.DriverId   = new SelectList(drivers2, "Id", "Name", booking.DriverId);
                    ViewBag.TourId     = new SelectList(activeTours2, "Id", "Title", booking.TourId);
                    ViewBag.TourPrices = activeTours2.ToDictionary(t => t.Id.ToString(), t => t.Price);
                    return View(booking);
                }

                if (!string.IsNullOrWhiteSpace(referralCode))
                {
                    var (valid, discountPct, _, referralCodeId) = await _referralService.ValidateCodeAsync(referralCode, currentUserId);
                    if (valid)
                    {
                        var tour = await _tourService.GetByIdAsync(booking.TourId);
                        if (tour != null)
                        {
                            booking.DiscountAmount = Math.Round(tour.Price * (discountPct / 100m), 2);
                            booking.ReferralCodeId = referralCodeId;
                        }
                    }
                }

                string adminEmail = _config["AdminNotificationEmail"];
                try
                {
                    await _bookingService.AddAsync(booking, adminEmail);

                    if (!string.IsNullOrWhiteSpace(referralCode) && booking.DiscountAmount > 0)
                        await _referralService.RecordUsageAsync(referralCode, booking.Id, currentUserId, booking.DiscountAmount.Value);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Something went wrong while saving your booking. Please try again.");
                    var driversE = await _driverService.GetActiveAsync();
                    var toursE   = await _tourService.GetActiveAsync();
                    ViewBag.DriverId   = new SelectList(driversE, "Id", "Name", booking.DriverId);
                    ViewBag.TourId     = new SelectList(toursE,   "Id", "Title", booking.TourId);
                    ViewBag.TourPrices = toursE.ToDictionary(t => t.Id.ToString(), t => t.Price);
                    return View(booking);
                }

                if (User.IsInRole("Administrator"))
                    return RedirectToAction("AdministratorIndex");
                else if (User.IsInRole("Driver"))
                    return RedirectToAction("DriverScheduler");
                else
                    return RedirectToAction("UserBookings");
            }

            var drivers     = await _driverService.GetActiveAsync();
            var activeTours = await _tourService.GetActiveAsync();
            ViewBag.DriverId   = new SelectList(drivers, "Id", "Name", booking.DriverId);
            ViewBag.TourId     = new SelectList(activeTours, "Id", "Title", booking.TourId);
            ViewBag.TourPrices = activeTours.ToDictionary(t => t.Id.ToString(), t => t.Price);
            return View(booking);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();
            var booking = await _bookingService.GetByIdAsync(id.Value);
            if (booking == null) return NotFound();
            var drivers = await _driverService.GetActiveAsync();
            var tours   = await _tourService.GetActiveAsync();
            ViewBag.DriverId = new SelectList(drivers, "Id", "Name", booking.DriverId);
            ViewBag.TourId   = new SelectList(tours,   "Id", "Title", booking.TourId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit([Bind("Id,NameOfBookMaker,TourId,DriverId,CustomerEmail,PhoneNumber,NumberOfPeople,BookingDateTime,Status")] BookingTour booking)
        {
            if (ModelState.IsValid)
            {
                await _bookingService.UpdateAsync(booking);
                return RedirectToAction("Index");
            }
            var drivers = await _driverService.GetActiveAsync();
            var tours   = await _tourService.GetActiveAsync();
            ViewBag.DriverId = new SelectList(drivers, "Id", "Name", booking.DriverId);
            ViewBag.TourId   = new SelectList(tours,   "Id", "Title", booking.TourId);
            return View(booking);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();
            var booking = await _bookingService.GetByIdAsync(id.Value);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _bookingService.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var (confirmationText, emailError) = await _bookingService.ApproveAsync(id);
            var booking = await _bookingService.GetByIdAsync(id);

            TempData["ApprovedBookingEmail"] = booking?.CustomerEmail;
            TempData["ApprovedBookingText"]  = confirmationText;
            TempData["ApprovedEmailError"]   = emailError;

            return RedirectToAction("AdministratorIndex");
        }

        [Authorize]
        public async Task<IActionResult> UserBookings()
        {
            string userEmail = User.Identity.Name;
            return View(await _bookingService.GetByUserEmailAsync(userEmail));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CanceledBooking(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id);
            if (booking == null) return NotFound();
            if (booking.CustomerEmail != User.Identity.Name && !User.IsInRole("Administrator"))
                return Forbid();
            await _bookingService.CancelAsync(id);
            return RedirectToAction("UserBookings");
        }

        [Authorize(Roles = "Administrator,Driver")]
        public async Task<JsonResult> GetApprovedBookings()
        {
            var items = await _bookingService.GetApprovedCalendarItemsAsync();
            return Json(items);
        }

        [Authorize(Roles = "Driver,Administrator")]
        public async Task<IActionResult> DriverScheduler()
        {
            string driverEmail = User.Identity.Name;
            var bookings = await _bookingService.GetByDriverEmailAsync(driverEmail);
            return View(bookings);
        }

        [Authorize]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id);
            if (booking == null) return NotFound();
            bool isOwner = booking.CustomerEmail == User.Identity.Name;
            if (!isOwner && !User.IsInRole("Administrator")) return Forbid();
            var pdf = _receiptService.GenerateBookingReceipt(booking);
            return File(pdf, "application/pdf", $"taxiblitz-receipt-{id}.pdf");
        }
    }
}

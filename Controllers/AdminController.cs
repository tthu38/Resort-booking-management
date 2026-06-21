using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResortBookingMVC.Data;
using ResortBookingMVC.Models;
using ResortBookingMVC.Models.Enums;

namespace ResortBookingMVC.Controllers
{
    [Authorize(Roles = "ADMIN,STAFF")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ───── DASHBOARD ─────
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalResorts  = await _context.Resorts.CountAsync(r => r.IsActive);
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalUsers    = await _context.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalRevenue  = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            ViewBag.RecentBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Resort)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View();
        }

        // ───── BOOKINGS ─────
        public async Task<IActionResult> Bookings(string? status, string? search, int page = 1)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Resort)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var bs))
                query = query.Where(b => b.Status == bs);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.BookingCode.Contains(search) ||
                                         b.User.FullName.Contains(search) ||
                                         b.User.Email.Contains(search));

            int pageSize = 15;
            ViewBag.TotalCount = await query.CountAsync();
            ViewBag.Page       = page;
            ViewBag.PageSize   = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(ViewBag.TotalCount / (double)pageSize);
            ViewBag.Status     = status;
            ViewBag.Search     = search;

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(bookings);
        }

        // POST: confirm booking
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(long id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            booking.Status    = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xác nhận booking {booking.BookingCode}.";
            return RedirectToAction("Bookings");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInBooking(long id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            booking.Status      = BookingStatus.CheckedIn;
            booking.CheckedInAt = DateTime.UtcNow;
            booking.UpdatedAt   = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Check-in thành công: {booking.BookingCode}.";
            return RedirectToAction("Bookings");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutBooking(long id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            booking.Status       = BookingStatus.Completed;
            booking.CheckedOutAt = DateTime.UtcNow;
            booking.UpdatedAt    = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Check-out thành công: {booking.BookingCode}.";
            return RedirectToAction("Bookings");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(long id, string? reason)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            booking.Status          = BookingStatus.Rejected;
            booking.CancelledReason = reason;
            booking.CancelledAt     = DateTime.UtcNow;
            booking.UpdatedAt       = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã từ chối booking {booking.BookingCode}.";
            return RedirectToAction("Bookings");
        }

        // ───── RESORTS ─────
        public async Task<IActionResult> Resorts()
        {
            var resorts = await _context.Resorts
                .Include(r => r.Location)
                .Include(r => r.RoomTypes)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(resorts);
        }

        [HttpGet]
        public async Task<IActionResult> CreateResort()
        {
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).ToListAsync();
            return View(new Resort());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResort(Resort model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).ToListAsync();
                return View(model);
            }
            model.Slug      = GenerateSlug(model.Name);
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            _context.Resorts.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm resort thành công.";
            return RedirectToAction("Resorts");
        }

        [HttpGet]
        public async Task<IActionResult> EditResort(int id)
        {
            var resort = await _context.Resorts.FindAsync(id);
            if (resort == null) return NotFound();
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).ToListAsync();
            return View(resort);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResort(Resort model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).ToListAsync();
                return View(model);
            }
            var resort = await _context.Resorts.FindAsync(model.Id);
            if (resort == null) return NotFound();
            resort.Name         = model.Name;
            resort.Description  = model.Description;
            resort.Address      = model.Address;
            resort.StarRating   = model.StarRating;
            resort.ThumbnailUrl = model.ThumbnailUrl;
            resort.LocationId   = model.LocationId;
            resort.IsActive     = model.IsActive;
            resort.CheckInTime  = model.CheckInTime;
            resort.CheckOutTime = model.CheckOutTime;
            resort.UpdatedAt    = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật resort.";
            return RedirectToAction("Resorts");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResort(int id)
        {
            var resort = await _context.Resorts.FindAsync(id);
            if (resort == null) return NotFound();
            resort.IsActive  = false;
            resort.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa resort.";
            return RedirectToAction("Resorts");
        }

        // ───── ROOMS ─────
        public async Task<IActionResult> RoomTypes(int resortId)
        {
            var resort = await _context.Resorts.FindAsync(resortId);
            if (resort == null) return NotFound();
            ViewBag.Resort = resort;

            var roomTypes = await _context.RoomTypes
                .Where(rt => rt.ResortId == resortId)
                .Include(rt => rt.Rooms)
                .ToListAsync();

            return View(roomTypes);
        }

        [HttpGet]
        public IActionResult CreateRoomType(int resortId)
        {
            ViewBag.ResortId = resortId;
            return View(new RoomType { ResortId = resortId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoomType(RoomType model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ResortId = model.ResortId;
                return View(model);
            }
            _context.RoomTypes.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm loại phòng.";
            return RedirectToAction("RoomTypes", new { resortId = model.ResortId });
        }

        // ───── USERS ─────
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Users(string? search, int page = 1)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            int pageSize = 20;
            ViewBag.TotalCount = await query.CountAsync();
            ViewBag.Page       = page;
            ViewBag.TotalPages = (int)Math.Ceiling(ViewBag.TotalCount / (double)pageSize);
            ViewBag.Search     = search;

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(users);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive  = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã {(user.IsActive ? "kích hoạt" : "vô hiệu hóa")} tài khoản.";
            return RedirectToAction("Users");
        }

        // ───── REVIEWS ─────
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Resort)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reviews);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReview(long id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            review.IsApproved = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Reviews");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(long id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return RedirectToAction("Reviews");
        }

        private static string GenerateSlug(string name)
            => name.ToLowerInvariant()
                   .Replace(" ", "-")
                   .Replace("đ", "d")
                   + "-" + Random.Shared.Next(1000, 9999);
    }
}

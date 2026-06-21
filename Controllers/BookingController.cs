using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResortBookingMVC.Data;
using ResortBookingMVC.Models;
using ResortBookingMVC.Models.Enums;
using ResortBookingMVC.ViewModels;
using System.Security.Claims;

namespace ResortBookingMVC.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Booking/Create
        public async Task<IActionResult> Create(int resortId, int roomTypeId, string checkIn, string checkOut, int rooms = 1, int adults = 2, int children = 0)
        {
            if (!DateOnly.TryParse(checkIn, out var checkInDate) ||
                !DateOnly.TryParse(checkOut, out var checkOutDate) ||
                checkOutDate <= checkInDate)
            {
                TempData["Error"] = "Ngày check-in/check-out không hợp lệ.";
                return RedirectToAction("Detail", "Resort", new { id = resortId });
            }

            var resort = await _context.Resorts
                .Include(r => r.Location)
                .Include(r => r.Services.Where(s => s.IsActive))
                .FirstOrDefaultAsync(r => r.Id == resortId && r.IsActive);

            var roomType = await _context.RoomTypes
                .Include(rt => rt.Images)
                .FirstOrDefaultAsync(rt => rt.Id == roomTypeId && rt.IsActive);

            if (resort == null || roomType == null)
                return NotFound();

            int totalNights = checkOutDate.DayNumber - checkInDate.DayNumber;
            decimal subTotal = roomType.BasePricePerNight * totalNights * rooms;
            decimal deposit = Math.Round(subTotal * (roomType.DepositPercentage / 100m), 0);

            var vm = new CreateBookingViewModel
            {
                ResortId = resortId,
                RoomTypeId = roomTypeId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumRooms = rooms,
                NumAdults = adults,
                NumChildren = children,
                Resort = resort,
                RoomType = roomType,
                Services = resort.Services.ToList(),
                TotalNights = totalNights,
                EstimatedTotal = subTotal,
                DepositAmount = deposit
            };

            return View(vm);
        }

        // POST: /Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            if (!DateOnly.TryParse(model.CheckInDate, out var checkInDate) ||
                !DateOnly.TryParse(model.CheckOutDate, out var checkOutDate) ||
                checkOutDate <= checkInDate)
            {
                ModelState.AddModelError("", "Ngày check-in/check-out không hợp lệ.");
                return await ReloadCreateView(model);
            }

            // Check room availability
            var bookedRoomIds = await _context.BookingRooms
                .Where(br =>
                    br.RoomType.ResortId == model.ResortId &&
                    br.Booking.Status != BookingStatus.Cancelled &&
                    br.Booking.Status != BookingStatus.Rejected &&
                    br.Booking.CheckInDate < checkOutDate &&
                    br.Booking.CheckOutDate > checkInDate)
                .Select(br => br.RoomId)
                .ToListAsync();

            var availableRooms = await _context.Rooms
                .Where(r =>
                    r.RoomTypeId == model.RoomTypeId &&
                    r.IsActive &&
                    r.Status == RoomStatus.Available &&
                    !bookedRoomIds.Contains(r.Id))
                .Take(model.NumRooms)
                .ToListAsync();

            if (availableRooms.Count < model.NumRooms)
            {
                ModelState.AddModelError("", "Không đủ phòng trống cho ngày bạn chọn.");
                return await ReloadCreateView(model);
            }

            var roomType = await _context.RoomTypes.FindAsync(model.RoomTypeId);
            if (roomType == null) return NotFound();

            int totalNights = checkOutDate.DayNumber - checkInDate.DayNumber;
            decimal subTotal = roomType.BasePricePerNight * totalNights * model.NumRooms;

            // Calculate service total
            decimal serviceTotal = 0;
            var selectedServices = new List<Service>();
            if (model.SelectedServiceIds?.Any() == true)
            {
                selectedServices = await _context.Services
                    .Where(s => model.SelectedServiceIds.Contains(s.Id))
                    .ToListAsync();
                serviceTotal = selectedServices.Sum(s => s.Price);
            }

            decimal totalAmount = subTotal + serviceTotal;
            decimal depositAmount = model.PaymentType == "Deposit"
                ? Math.Round(totalAmount * (roomType.DepositPercentage / 100m), 0)
                : totalAmount;

            var booking = new Booking
            {
                BookingCode = GenerateBookingCode(),
                UserId = CurrentUserId,
                ResortId = model.ResortId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                TotalNights = totalNights,
                NumRooms = model.NumRooms,
                NumAdults = model.NumAdults,
                NumChildren = model.NumChildren,
                SpecialRequest = model.SpecialRequest,
                Status = BookingStatus.Pending,
                SubTotal = subTotal,
                ServiceTotal = serviceTotal,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Add booking rooms
            foreach (var room in availableRooms)
            {
                _context.BookingRooms.Add(new BookingRoom
                {
                    BookingId = booking.Id,
                    RoomId = room.Id,
                    RoomTypeId = model.RoomTypeId,
                    PricePerNight = roomType.BasePricePerNight,
                    TotalPrice = roomType.BasePricePerNight * totalNights
                });
            }

            // Add booking services
            foreach (var svc in selectedServices)
            {
                _context.BookingServices.Add(new BookingService
                {
                    BookingId = booking.Id,
                    ServiceId = svc.Id,
                    Quantity = 1,
                    UnitPrice = svc.Price,
                    TotalPrice = svc.Price
                });
            }

            await _context.SaveChangesAsync();

            TempData["BookingCode"] = booking.BookingCode;
            return RedirectToAction("Success", new { id = booking.Id });
        }

        // GET: /Booking/Success/5
        public async Task<IActionResult> Success(long id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Resort).ThenInclude(r => r.Location)
                .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
                .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == CurrentUserId);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // GET: /Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            var bookings = await _context.Bookings
                .Where(b => b.UserId == CurrentUserId)
                .Include(b => b.Resort).ThenInclude(r => r.Location)
                .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // GET: /Booking/Detail/5
        public async Task<IActionResult> Detail(long id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Resort).ThenInclude(r => r.Location)
                .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
                .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .Include(b => b.Payments)
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == CurrentUserId);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // POST: /Booking/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id, string? reason)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == CurrentUserId);

            if (booking == null) return NotFound();

            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["Error"] = "Booking này đã được hủy trước đó.";
                return RedirectToAction("Detail", new { id });
            }

            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.Completed)
            {
                TempData["Error"] = "Không thể hủy booking đã check-in hoặc hoàn thành.";
                return RedirectToAction("Detail", new { id });
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelledReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã hủy booking thành công.";
            return RedirectToAction("MyBookings");
        }

        // GET: /Booking/Review/5
        public async Task<IActionResult> WriteReview(long id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Resort)
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == CurrentUserId);

            if (booking == null) return NotFound();
            if (booking.Status != BookingStatus.Completed)
            {
                TempData["Error"] = "Chỉ có thể đánh giá sau khi hoàn thành.";
                return RedirectToAction("Detail", new { id });
            }
            if (booking.Review != null)
            {
                TempData["Error"] = "Bạn đã đánh giá booking này rồi.";
                return RedirectToAction("Detail", new { id });
            }

            ViewBag.Booking = booking;
            return View(new Review { BookingId = id, ResortId = booking.ResortId });
        }

        // POST: /Booking/WriteReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriteReview(Review model)
        {
            model.UserId = CurrentUserId;
            model.IsApproved = true;
            _context.Reviews.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("Detail", new { id = model.BookingId });
        }

        private async Task<IActionResult> ReloadCreateView(CreateBookingViewModel model)
        {
            model.Resort = await _context.Resorts
                .Include(r => r.Location)
                .Include(r => r.Services.Where(s => s.IsActive))
                .FirstOrDefaultAsync(r => r.Id == model.ResortId);
            model.RoomType = await _context.RoomTypes.Include(rt => rt.Images).FirstOrDefaultAsync(rt => rt.Id == model.RoomTypeId);
            model.Services = model.Resort?.Services?.ToList() ?? new();
            return View(model);
        }

        private static string GenerateBookingCode()
            => $"RES-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
    }
}

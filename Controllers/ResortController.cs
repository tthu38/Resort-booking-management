using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResortBookingMVC.Data;
using ResortBookingMVC.ViewModels;

namespace ResortBookingMVC.Controllers
{
    public class ResortController : Controller
    {
        private readonly AppDbContext _context;

        public ResortController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Resort/Search
        public async Task<IActionResult> Search(ResortSearchViewModel model)
        {
            var query = _context.Resorts
                .Where(r => r.IsActive)
                .Include(r => r.Location)
                .Include(r => r.Images)
                .Include(r => r.RoomTypes)
                .Include(r => r.Reviews)
                .AsQueryable();

            // Filter by location keyword
            if (!string.IsNullOrWhiteSpace(model.Location))
                query = query.Where(r =>
                    r.Location.Province.Contains(model.Location) ||
                    r.Address!.Contains(model.Location) ||
                    r.Name.Contains(model.Location));

            // Filter by price
            if (model.MinPrice.HasValue)
                query = query.Where(r => r.RoomTypes.Any(rt => rt.BasePricePerNight >= model.MinPrice));
            if (model.MaxPrice.HasValue)
                query = query.Where(r => r.RoomTypes.Any(rt => rt.BasePricePerNight <= model.MaxPrice));

            // Filter by star rating
            if (model.StarRating.HasValue)
                query = query.Where(r => r.StarRating == model.StarRating);

            // Filter by date availability (checkIn & checkOut)
            if (!string.IsNullOrEmpty(model.CheckIn) && !string.IsNullOrEmpty(model.CheckOut)
                && DateOnly.TryParse(model.CheckIn, out var checkIn)
                && DateOnly.TryParse(model.CheckOut, out var checkOut))
            {
                var bookedResortIds = _context.Bookings
                    .Where(b => b.Status != Models.Enums.BookingStatus.Cancelled
                        && b.Status != Models.Enums.BookingStatus.Rejected
                        && b.CheckInDate < checkOut
                        && b.CheckOutDate > checkIn)
                    .Select(b => b.ResortId)
                    .Distinct();

                query = query.Where(r => !bookedResortIds.Contains(r.Id)
                    || r.RoomTypes.Any(rt =>
                        rt.IsActive &&
                        rt.Rooms.Count(room => room.IsActive &&
                            !_context.BookingRooms.Any(br =>
                                br.RoomId == room.Id &&
                                br.Booking.Status != Models.Enums.BookingStatus.Cancelled &&
                                br.Booking.CheckInDate < checkOut &&
                                br.Booking.CheckOutDate > checkIn))
                        >= model.Rooms));
            }

            // Sort
            query = model.SortBy switch
            {
                "price_asc" => query.OrderBy(r => r.RoomTypes.Min(rt => (decimal?)rt.BasePricePerNight)),
                "price_desc" => query.OrderByDescending(r => r.RoomTypes.Min(rt => (decimal?)rt.BasePricePerNight)),
                "rating" => query.OrderByDescending(r => r.StarRating),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            model.TotalItems = await query.CountAsync();

            model.Results = await query
                .Skip((model.Page - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            model.Locations = await _context.Locations.Where(l => l.IsActive).ToListAsync();

            return View(model);
        }

        // GET: /Resort/Detail/5
        public async Task<IActionResult> Detail(int id, string? checkIn, string? checkOut, int adults = 2, int children = 0, int rooms = 1)
        {
            var resort = await _context.Resorts
                .Include(r => r.Location)
                .Include(r => r.Images)
                .Include(r => r.Services.Where(s => s.IsActive))
                .Include(r => r.CancellationPolicies)
                .Include(r => r.Reviews.Where(rv => rv.IsApproved))
                    .ThenInclude(rv => rv.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (resort == null) return NotFound();

            var vm = new ResortDetailViewModel
            {
                Resort = resort,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Adults = adults,
                Children = children,
                Rooms = rooms,
                Reviews = resort.Reviews.ToList(),
                AverageRating = resort.Reviews.Any() ? resort.Reviews.Average(r => r.Rating) : 0
            };

            // Get available room types if dates provided
            if (!string.IsNullOrEmpty(checkIn) && !string.IsNullOrEmpty(checkOut)
                && DateOnly.TryParse(checkIn, out var checkInDate)
                && DateOnly.TryParse(checkOut, out var checkOutDate))
            {
                var bookedRoomIds = await _context.BookingRooms
                    .Where(br =>
                        br.Booking.ResortId == id &&
                        br.Booking.Status != Models.Enums.BookingStatus.Cancelled &&
                        br.Booking.Status != Models.Enums.BookingStatus.Rejected &&
                        br.Booking.CheckInDate < checkOutDate &&
                        br.Booking.CheckOutDate > checkInDate)
                    .Select(br => br.RoomId)
                    .ToListAsync();

                vm.AvailableRoomTypes = await _context.RoomTypes
                    .Where(rt =>
                        rt.ResortId == id &&
                        rt.IsActive &&
                        rt.MaxAdults >= adults &&
                        (rt.MaxAdults + rt.MaxChildren) >= (adults + children) &&
                        rt.Rooms.Any(r => r.IsActive &&
                            r.Status == Models.Enums.RoomStatus.Available &&
                            !bookedRoomIds.Contains(r.Id)))
                    .Include(rt => rt.Images)
                    .ToListAsync();
            }
            else
            {
                vm.AvailableRoomTypes = await _context.RoomTypes
                    .Where(rt => rt.ResortId == id && rt.IsActive)
                    .Include(rt => rt.Images)
                    .ToListAsync();
            }

            return View(vm);
        }
    }
}

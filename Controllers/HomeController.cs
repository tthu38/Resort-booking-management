using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResortBookingMVC.Data;
using ResortBookingMVC.ViewModels;

namespace ResortBookingMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /
        public async Task<IActionResult> Index()
        {
            var featuredResorts = await _context.Resorts
                .Where(r => r.IsActive)
                .Include(r => r.Location)
                .Include(r => r.Images)
                .Include(r => r.RoomTypes)
                .Include(r => r.Reviews)
                .OrderByDescending(r => r.StarRating)
                .Take(6)
                .ToListAsync();

            var locations = await _context.Locations
                .Where(l => l.IsActive)
                .ToListAsync();

            ViewBag.FeaturedResorts = featuredResorts;
            ViewBag.Locations = locations;
            return View();
        }

        // GET: /Home/About
        public IActionResult About() => View();

        // GET: /Home/Contact
        public IActionResult Contact() => View();

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}

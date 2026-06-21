using ResortBookingMVC.Models;

namespace ResortBookingMVC.ViewModels
{
    public class ResortSearchViewModel
    {
        public string? Location { get; set; }
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public int Rooms { get; set; } = 1;
        public int Adults { get; set; } = 2;
        public int Children { get; set; } = 0;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? StarRating { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public List<Resort> Results { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
    }
}

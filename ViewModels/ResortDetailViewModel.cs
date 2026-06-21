using ResortBookingMVC.Models;

namespace ResortBookingMVC.ViewModels
{
    public class ResortDetailViewModel
    {
        public Resort Resort { get; set; } = null!;
        public List<RoomType> AvailableRoomTypes { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public int Adults { get; set; } = 2;
        public int Children { get; set; } = 0;
        public int Rooms { get; set; } = 1;
    }
}

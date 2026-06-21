using System.ComponentModel.DataAnnotations;
using ResortBookingMVC.Models;

namespace ResortBookingMVC.ViewModels
{
    public class CreateBookingViewModel
    {
        [Required] public int ResortId { get; set; }
        [Required] public int RoomTypeId { get; set; }
        [Required] public string CheckInDate { get; set; } = null!;
        [Required] public string CheckOutDate { get; set; } = null!;
        [Range(1, 10)] public int NumRooms { get; set; } = 1;
        [Range(1, 20)] public int NumAdults { get; set; } = 1;
        [Range(0, 10)] public int NumChildren { get; set; } = 0;
        public string PaymentType { get; set; } = "Deposit";
        public string? SpecialRequest { get; set; }
        public List<int> SelectedServiceIds { get; set; } = new();
        // Display
        public Resort? Resort { get; set; }
        public RoomType? RoomType { get; set; }
        public List<Service> Services { get; set; } = new();
        public decimal EstimatedTotal { get; set; }
        public decimal DepositAmount { get; set; }
        public int TotalNights { get; set; }
    }
}

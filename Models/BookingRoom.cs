using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class BookingRoom
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required] public long BookingId { get; set; }
        [Required] public int RoomId { get; set; }
        [Required] public int RoomTypeId { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal PricePerNight { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal TotalPrice { get; set; }
        public Booking Booking { get; set; } = null!;
        public Room Room { get; set; } = null!;
        public RoomType RoomType { get; set; } = null!;
    }
}

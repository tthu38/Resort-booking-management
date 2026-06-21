using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class BookingService
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required] public long BookingId { get; set; }
        [Required] public int ServiceId { get; set; }
        public int Quantity { get; set; } = 1;
        [Column(TypeName = "decimal(12,2)")] public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal TotalPrice { get; set; }
        public Booking Booking { get; set; } = null!;
        public Service Service { get; set; } = null!;
    }
}

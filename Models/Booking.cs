using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ResortBookingMVC.Models.Enums;

namespace ResortBookingMVC.Models
{
    public class Booking
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required, MaxLength(20)] public string BookingCode { get; set; } = null!;
        [Required] public long UserId { get; set; }
        [Required] public int ResortId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int TotalNights { get; set; }
        public int NumRooms { get; set; } = 1;
        public int NumAdults { get; set; } = 1;
        public int NumChildren { get; set; } = 0;
        public string? SpecialRequest { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        [Column(TypeName = "decimal(14,2)")] public decimal SubTotal { get; set; }
        [Column(TypeName = "decimal(14,2)")] public decimal ServiceTotal { get; set; } = 0;
        [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; } = 0;
        [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
        [Column(TypeName = "decimal(14,2)")] public decimal DepositAmount { get; set; } = 0;
        [Column(TypeName = "decimal(14,2)")] public decimal PaidAmount { get; set; } = 0;
        public DateTime? CancelledAt { get; set; }
        public string? CancelledReason { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public DateTime? CheckedOutAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Navigation
        public User User { get; set; } = null!;
        public Resort Resort { get; set; } = null!;
        public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
        public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public Review? Review { get; set; }
    }
}

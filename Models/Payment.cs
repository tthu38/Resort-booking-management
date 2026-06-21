using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ResortBookingMVC.Models.Enums;

namespace ResortBookingMVC.Models
{
    public class Payment
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required] public long BookingId { get; set; }
        [Required, MaxLength(100)] public string TransactionCode { get; set; } = null!;
        [Column(TypeName = "decimal(14,2)")] public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? GatewayResponse { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Booking Booking { get; set; } = null!;
    }
}

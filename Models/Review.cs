using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class Review
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required] public long BookingId { get; set; }
        [Required] public long UserId { get; set; }
        [Required] public int ResortId { get; set; }
        [Range(1, 5)] public byte Rating { get; set; }
        [MaxLength(2000)] public string? Comment { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Booking Booking { get; set; } = null!;
        public User User { get; set; } = null!;
        public Resort Resort { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class Resort
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] public int LocationId { get; set; }
        [Required, MaxLength(200)] public string Name { get; set; } = null!;
        [Required, MaxLength(250)] public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        [MaxLength(300)] public string? Address { get; set; }
        [Column(TypeName = "decimal(9,6)")] public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(9,6)")] public decimal? Longitude { get; set; }
        public byte? StarRating { get; set; }
        public TimeOnly CheckInTime { get; set; } = new TimeOnly(14, 0);
        public TimeOnly CheckOutTime { get; set; } = new TimeOnly(12, 0);
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Navigation
        public Location Location { get; set; } = null!;
        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
        public ICollection<ResortImage> Images { get; set; } = new List<ResortImage>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
        public ICollection<CancellationPolicy> CancellationPolicies { get; set; } = new List<CancellationPolicy>();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class RoomType
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] public int ResortId { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int MaxAdults { get; set; } = 2;
        public int MaxChildren { get; set; } = 1;
        [Column(TypeName = "decimal(6,2)")] public decimal? AreaSqm { get; set; }
        [MaxLength(100)] public string? BedType { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal BasePricePerNight { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal DepositPercentage { get; set; } = 30.00m;
        public bool IsActive { get; set; } = true;
        // Navigation
        public Resort Resort { get; set; } = null!;
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
        public ICollection<PricingRule> PricingRules { get; set; } = new List<PricingRule>();
        [MaxLength(500)] public string? ThumbnailUrl { get; set; }  
    }
}

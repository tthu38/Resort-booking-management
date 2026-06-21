using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class PricingRule
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] public int RoomTypeId { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = null!;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal Price { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public RoomType RoomType { get; set; } = null!;
    }
}

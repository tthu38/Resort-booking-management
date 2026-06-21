using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class Service
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] public int ResortId { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
        public Resort Resort { get; set; } = null!;
        public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    }
}

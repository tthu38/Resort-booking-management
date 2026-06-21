using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class Location
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Province { get; set; } = null!;
        [MaxLength(100)] public string? District { get; set; }
        [Required, MaxLength(150)] public string Slug { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public ICollection<Resort> Resorts { get; set; } = new List<Resort>();
    }
}

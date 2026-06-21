using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class ResortImage
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] public int ResortId { get; set; }
        [Required] public string ImageUrl { get; set; } = null!;
        public string? Caption { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsThumbnail { get; set; } = false;
        public Resort Resort { get; set; } = null!;
    }
}

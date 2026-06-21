using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResortBookingMVC.Models
{
    public class CancellationPolicy
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] public int ResortId { get; set; }
        public int DaysBeforeCheckIn { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal RefundPercentage { get; set; }
        [MaxLength(500)] public string? Description { get; set; }
        public Resort Resort { get; set; } = null!;
    }
}

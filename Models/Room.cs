using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ResortBookingMVC.Models.Enums;

namespace ResortBookingMVC.Models
{
    public class Room
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required] public int RoomTypeId { get; set; }
        [Required] public int ResortId { get; set; }
        [Required, MaxLength(20)] public string RoomNumber { get; set; } = null!;
        public int? Floor { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Available;
        public bool IsActive { get; set; } = true;
        public RoomType RoomType { get; set; } = null!;
        public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
    }
}

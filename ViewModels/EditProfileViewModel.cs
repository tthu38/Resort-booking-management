using System.ComponentModel.DataAnnotations;

namespace ResortBookingMVC.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(150)]
        public string Email { get; set; } = null!;

        [MaxLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        // Để trống nếu không muốn đổi mật khẩu
        [MinLength(6, ErrorMessage = "Mật khẩu phải ít nhất 6 ký tự")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ConfirmPassword { get; set; }
    }
}
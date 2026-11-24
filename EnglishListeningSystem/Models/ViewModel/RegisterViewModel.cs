using System.ComponentModel.DataAnnotations;

namespace EnglishListeningSystem.Models.ViewModel
{
    public class RegisterViewModel
    {
        private string _tenDangNhap = string.Empty;
        private string _email = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string HoTen { get; set; }

        // Bỏ khoảng trắng ở hai đầu trước khi MVC kiểm tra độ dài và định dạng.
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50)]
        public string Username
        {
            get { return _tenDangNhap; }
            set { _tenDangNhap = value?.Trim() ?? string.Empty; }
        }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        public string Email
        {
            get { return _email; }
            set { _email = value?.Trim() ?? string.Empty; }
        }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6,ErrorMessage ="Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required(ErrorMessage ="Bạn chưa xác nhận mật khẩu")]
        [Compare("Password",ErrorMessage ="Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string VaiTro { get; set; } = "Student";
    }
}

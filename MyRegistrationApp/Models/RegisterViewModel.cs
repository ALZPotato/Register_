using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MyRegistrationApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chuyên ngành.")] // Có thể làm bắt buộc hoặc không tùy bạn
        [Display(Name = "Chuyên ngành")]
        public string Business { get; set; }

        public IEnumerable<SelectListItem> MajorOptions { get; set; }

        // Constructor để khởi tạo danh sách chuyên ngành (tùy chọn, có thể làm trong Controller)
        public RegisterViewModel()
        {
            MajorOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Công nghệ thông tin", Text = "Công nghệ thông tin" },
                new SelectListItem { Value = "Công nghệ phần mềm", Text = "Công nghệ phần mềm" },
                new SelectListItem { Value = "An ninh mạng", Text = "An ninh mạng" }
            };
        }
    }
}
using System.ComponentModel.DataAnnotations;
using MyRegistrationApp.Models; 

namespace MyRegistrationApp.Models
{
    public class OrderViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
        [StringLength(100)]
        [Display(Name = "Họ và Tên Người Nhận")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(20)]
        [Phone]
        [Display(Name = "Số Điện Thoại")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(500)]
        [Display(Name = "Địa Chỉ Giao Hàng")]
        public string Address { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email (để nhận thông tin đơn hàng)")]
        public string Email { get; set; }

        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Ghi Chú (tùy chọn)")]
        public string Notes { get; set; }

        public CartViewModel Cart { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; }
    }
}
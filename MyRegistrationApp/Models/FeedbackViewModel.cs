using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MyRegistrationApp.Models 
{
    public class FeedbackViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên của bạn.")]
        [StringLength(100)]
        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(100)]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Số điện thoại (tùy chọn)")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại phản hồi.")]
        [Display(Name = "Loại phản hồi")]
        public string FeedbackType { get; set; } 

        public System.Collections.Generic.IEnumerable<System.Web.Mvc.SelectListItem> FeedbackTypeOptions { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập tiêu đề phản hồi.")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Nội dung phản hồi")]
        public string Message { get; set; }

        [Display(Name = "Đính kèm ảnh (tùy chọn, tối đa 5MB, định dạng: jpg, png, gif)")]
        public HttpPostedFileBase AttachedImageFile { get; set; }

        [Display(Name = "Tôi đồng ý cho The Coffee liên hệ lại (nếu cần)")]
        public bool AllowContact { get; set; }

        public FeedbackViewModel()
        {
            FeedbackTypeOptions = new System.Collections.Generic.List<System.Web.Mvc.SelectListItem>
            {
                new System.Web.Mvc.SelectListItem { Value = "Góp ý", Text = "Góp ý chung" },
                new System.Web.Mvc.SelectListItem { Value = "Khiếu nại sản phẩm", Text = "Khiếu nại về sản phẩm" },
                new System.Web.Mvc.SelectListItem { Value = "Khiếu nại dịch vụ", Text = "Khiếu nại về dịch vụ" },
                new System.Web.Mvc.SelectListItem { Value = "Khen ngợi", Text = "Khen ngợi/Lời khen" },
                new System.Web.Mvc.SelectListItem { Value = "Khác", Text = "Vấn đề khác" }
            };
            AllowContact = true; 
        }
    }
}
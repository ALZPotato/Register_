using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MyRegistrationApp.Models
{
    public class ProductViewModel
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên Sản Phẩm")]
        public string ProductName { get; set; }

        [AllowHtml] // Quan trọng nếu bạn dự định dùng Rich Text Editor cho mô tả
        [DataType(DataType.MultilineText)]
        [Display(Name = "Mô Tả Chi Tiết")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc.")]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 1,000 VNĐ.")] // Điều chỉnh range nếu cần
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)] // Định dạng hiển thị số nguyên
        [Display(Name = "Giá Bán (VNĐ)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải là số không âm.")]
        [Display(Name = "Số Lượng Tồn Kho")]
        public int StockQuantity { get; set; }

        [Display(Name = "Ảnh Sản Phẩm Hiện Tại")]
        public string CurrentImageFileName { get; set; } // Dùng khi sửa, để hiển thị ảnh cũ

        [Display(Name = "Tải Ảnh Mới (ghi đè ảnh cũ, tối đa 5MB, định dạng: jpg, png, gif)")]
        public HttpPostedFileBase ProductImageFile { get; set; } // Để upload ảnh

        [Display(Name = "Danh Mục Sản Phẩm")]
        public int? CategoryID { get; set; } // Cho phép null nếu sản phẩm có thể không thuộc danh mục nào

        [Display(Name = "Kích Hoạt (Hiển thị sản phẩm)")]
        public bool IsActive { get; set; }

        // Thuộc tính này sẽ chứa danh sách các Category để người dùng chọn từ DropDownList
        public IEnumerable<SelectListItem> CategoryOptions { get; set; }

        public ProductViewModel()
        {
            IsActive = true; // Mặc định sản phẩm mới sẽ được kích hoạt
            CategoryOptions = new List<SelectListItem>(); // Khởi tạo danh sách rỗng
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyRegistrationApp.Models
{
    public class ProductsViewModel
    {
        public List<Category> Categories { get; set; }
        public List<Product> ProductsToList { get; set; } // THÊM DÒNG NÀY
        public string CurrentCategoryName { get; set; } // Để hiển thị tên danh mục đang chọn (tùy chọn)

        public ProductsViewModel()
        {
            Categories = new List<Category>();
            ProductsToList = new List<Product>(); // KHỞI TẠO
        }
    }
}
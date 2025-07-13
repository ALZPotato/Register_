using System;
using System.Collections.Generic;
using System.Web.Mvc; // Cho SelectListItem

namespace MyRegistrationApp.Models
{
    public class AdminOrderDetailViewModel
    {
        // Thông tin chung của đơn hàng
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public string CurrentStatus { get; set; }
        public decimal TotalAmount { get; set; }

        // Thông tin khách hàng
        public string CustomerFullName { get; set; }
        public string CustomerUsername { get; set; }

        // Thông tin giao hàng
        public string RecipientName { get; set; }
        public string ShippingAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }

        // Danh sách các sản phẩm trong đơn hàng (DÙNG List<OrderDetailViewModel>)
        public List<OrderDetailViewModel> Items { get; set; }

        // Dữ liệu cho DropDownList cập nhật trạng thái
        public IEnumerable<SelectListItem> StatusOptions { get; set; }

        public AdminOrderDetailViewModel()
        {
            Items = new List<OrderDetailViewModel>();
            StatusOptions = new List<SelectListItem>();
        }
    }
}
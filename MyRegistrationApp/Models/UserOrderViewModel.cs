using System;
using System.Collections.Generic;

namespace MyRegistrationApp.Models
{
    public class UserOrderViewModel
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string RecipientName { get; set; }
        public string ShippingAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; } 
        public string Notes { get; set; }
        public List<UserOrderDetailViewModel> Items { get; set; }

        public UserOrderViewModel()
        {
            Items = new List<UserOrderDetailViewModel>();
        }
    }
}
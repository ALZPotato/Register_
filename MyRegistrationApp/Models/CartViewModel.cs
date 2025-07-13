using System.Collections.Generic;
using System.Linq;

namespace MyRegistrationApp.Models
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public decimal TotalPrice => CartItems.Sum(item => item.Product.Price * item.Quantity);

        public CartViewModel()
        {
            CartItems = new List<CartItem>();
        }
    }
}
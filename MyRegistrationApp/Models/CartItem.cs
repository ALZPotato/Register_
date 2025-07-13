namespace MyRegistrationApp.Models
{
    public class CartItem
    {
        public Product Product { get; set; } // Lớp Product từ LINQ to SQL
        public int Quantity { get; set; }
    }
}
namespace MyRegistrationApp.Models
{
    public class UserOrderDetailViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductImageFileName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }
}
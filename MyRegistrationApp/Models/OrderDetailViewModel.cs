namespace MyRegistrationApp.Models
{
    public class OrderDetailViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price; // Thuộc tính tính toán tổng tiền cho dòng này
    }
}
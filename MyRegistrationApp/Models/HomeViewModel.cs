using System.Collections.Generic;

namespace MyRegistrationApp.Models
{
    public class HomeViewModel
    {
        
        public List<Product> FeaturedProducts { get; set; }

        public HomeViewModel()
        {
            FeaturedProducts = new List<Product>();
        }
    }
}
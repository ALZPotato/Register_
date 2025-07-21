using MyRegistrationApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;


namespace MyRegistrationApp.Controllers
{
    public class CartController : Controller
    {
        private UserDataDataContext _db;

        public CartController()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyWebAppDBConnectionString"].ConnectionString;
            _db = new UserDataDataContext(connectionString); 
        }

        // Hàm để lấy giỏ hàng từ Session hoặc Database
        private List<MyRegistrationApp.Models.CartItem> GetCart()
        {
            List<MyRegistrationApp.Models.CartItem> cartViewModel = new List<MyRegistrationApp.Models.CartItem>();

            if (User.Identity.IsAuthenticated)
            {
                // 1. Người dùng đã đăng nhập -> Lấy giỏ hàng từ DATABASE
                var user = _db.Users.SingleOrDefault(u => u.Username == User.Identity.Name); 
                if (user != null)
                {
                    // 2. Lấy danh sách các CartItem từ DB (đây là các đối tượng LINQ to SQL)
                    var dbCartItems = _db.CartItems.Where(c => c.UserID == user.UserID).ToList(); 

                    // 3. CHUYỂN ĐỔI (MAP) từ đối tượng LINQ to SQL sang đối tượng ViewModel
                    foreach (var dbItem in dbCartItems)
                    {
                        // Với mỗi item từ DB, tạo một item ViewModel tương ứng và thêm vào danh sách trả về
                        cartViewModel.Add(new MyRegistrationApp.Models.CartItem
                        {
                            Product = dbItem.Product, 
                            Quantity = dbItem.Quantity
                        });
                    }
                }
            }
            else
            {
                // 2. Người dùng chưa đăng nhập -> Lấy giỏ hàng từ SESSION
                cartViewModel = Session["Cart"] as List<MyRegistrationApp.Models.CartItem>;
                if (cartViewModel == null)
                {
                    cartViewModel = new List<MyRegistrationApp.Models.CartItem>();
                    Session["Cart"] = cartViewModel;
                }
            }

            return cartViewModel;
        }

        // Hàm để lưu giỏ hàng vào Session hoặc Database
        private void SaveCart(List<MyRegistrationApp.Models.CartItem> cart)
        {
            if (User.Identity.IsAuthenticated)
            {
                // Người dùng đã đăng nhập -> Lưu vào DATABASE
                var user = _db.Users.SingleOrDefault(u => u.Username == User.Identity.Name); 
                if (user != null)
                {
                    // Xóa tất cả các item cũ trong DB của user để đồng bộ lại
                    var oldItems = _db.CartItems.Where(ci => ci.UserID == user.UserID);
                    _db.CartItems.DeleteAllOnSubmit(oldItems);

                    // Thêm các item mới từ giỏ hàng hiện tại
                    var newDbItems = cart.Select(item => new CartItem 
                    {
                        UserID = user.UserID,
                        ProductID = item.Product.ProductID,
                        Quantity = item.Quantity,
                        DateAdded = DateTime.Now
                    });
                    _db.CartItems.InsertAllOnSubmit(newDbItems);

                    _db.SubmitChanges();
                }
            }
            else
            {
                // Người dùng chưa đăng nhập -> Lưu vào SESSION
                Session["Cart"] = cart;
            }
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            var cart = GetCart(); // Lấy giỏ hàng hiện tại (từ DB hoặc Session)
            var productToAdd = _db.Products.FirstOrDefault(p => p.ProductID == productId && p.IsActive); 

            if (productToAdd == null)
            {
                TempData["CartError"] = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.";
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));
            }

            var existingItem = cart.FirstOrDefault(item => item.Product.ProductID == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                // Sử dụng ViewModel CartItem ở đây
                cart.Add(new MyRegistrationApp.Models.CartItem { Product = productToAdd, Quantity = quantity });
            }

            SaveCart(cart); // Lưu lại giỏ hàng (vào DB hoặc Session)
            TempData["CartSuccess"] = $"Đã thêm '{productToAdd.ProductName}' vào giỏ hàng!";

            return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));
        }

        // GET: /Cart (Hiển thị trang giỏ hàng)
        public ActionResult Index()
        {
            var cart = GetCart();
            var viewModel = new CartViewModel { CartItems = cart };
            ViewBag.Title = "Giỏ Hàng Của Bạn";
            return View(viewModel);
        }

        // POST: /Cart/UpdateCart
        [HttpPost]
        public ActionResult UpdateCart(int productId, int quantity)
        {
            var cart = GetCart();
            var itemToUpdate = cart.FirstOrDefault(item => item.Product.ProductID == productId);
            if (itemToUpdate != null)
            {
                if (quantity > 0)
                {
                    itemToUpdate.Quantity = quantity;
                }
                else
                {
                    cart.Remove(itemToUpdate); // Nếu số lượng là 0 hoặc âm, xóa khỏi giỏ hàng
                }
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // GET: /Cart/RemoveFromCart/5
        public ActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var itemToRemove = cart.FirstOrDefault(item => item.Product.ProductID == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // Partial View để hiển thị tóm tắt giỏ hàng trên header
        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            var cart = GetCart();
            ViewBag.CartItemCount = cart.Sum(item => item.Quantity);
            return PartialView("_CartSummary");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
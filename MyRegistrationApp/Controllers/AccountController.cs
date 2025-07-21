// File: Controllers/AccountController.cs

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MyRegistrationApp.Models;
using System.Security.Principal;
using System.Text;
using System.Security.Cryptography;

namespace MyRegistrationApp.Controllers
{
    public class AccountController : Controller
    {
        private UserDataDataContext _db;

        public AccountController()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyWebAppDBConnectionString"].ConnectionString;
            _db = new UserDataDataContext(connectionString); 
        }

        // ... (Action Register của bạn) ...
        public ActionResult Register() { return View(new RegisterViewModel()); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {

                // 1. Kiểm tra xem username đã tồn tại chưa
                var existingUser = _db.Users.FirstOrDefault(u => u.Username == model.Username); 
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
                    return View(model);
                }

                // 2. Băm mật khẩu
                string hashedPassword = HashPassword(model.Password);

                // 3. Tạo đối tượng User mới
                User newUser = new User
                {
                    Username = model.Username,
                    PasswordHash = hashedPassword,
                    FullName = model.FullName,
                    Business = model.Business, 
                    RegisteredDate = DateTime.Now,
                    Role = "User" // Gán vai trò mặc định là "User"
                };

                // 4. Thêm user mới và lưu vào DB
                _db.Users.InsertOnSubmit(newUser); 
                try
                {
                    _db.SubmitChanges(); 
                    ViewBag.Message = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
                    return View("RegistrationSuccess"); // Chuyển đến trang thông báo thành công
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi vào file hoặc hệ thống log
                    System.Diagnostics.Debug.WriteLine("Lỗi khi đăng ký user: " + ex.ToString());
                    ModelState.AddModelError("", "Đã có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại sau.");
                }
            }

            // Nếu ModelState không hợp lệ ngay từ đầu hoặc có lỗi khi lưu DB, quay trở lại form
            return View(model);
        }


        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _db.Users.FirstOrDefault(u => u.Username == model.Username); 

            if (user != null)
            {
                string inputPasswordHash = HashPassword(model.Password);
                if (user.PasswordHash == inputPasswordHash)
                {
                    // Đăng nhập thành công, tạo cookie xác thực
                    string roles = user.Role;
                    FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(1, user.Username, DateTime.Now, DateTime.Now.AddMinutes(FormsAuthentication.Timeout.TotalMinutes), model.RememberMe, roles, FormsAuthentication.FormsCookiePath);
                    string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                    HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    if (model.RememberMe) { authCookie.Expires = authTicket.Expiration; }
                    authCookie.HttpOnly = true;
                    Response.Cookies.Add(authCookie);

                    // === GỌI HÀM HỢP NHẤT GIỎ HÀNG ===
                    MergeSessionCartWithDatabaseCart(user.UserID);
                    // ===================================

                    // Chuyển hướng
                    if (Url.IsLocalUrl(returnUrl) && !string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        if (roles != null && roles.Contains("Admin")) // Kiểm tra an toàn hơn
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }


        private void MergeSessionCartWithDatabaseCart(int userId)
        {
            // Lấy giỏ hàng tạm thời trong Session (khi chưa đăng nhập)
            List<CartItem> sessionCart = Session["Cart"] as List<CartItem>; // Đây là List<Models.CartItem> (ViewModel)

            if (sessionCart != null && sessionCart.Any())
            {
                // Lấy giỏ hàng đã lưu trong database của user
                var dbCartItems = _db.CartItems.Where(ci => ci.UserID == userId).ToList(); // Đây là List<LINQ_to_SQL.CartItem>

                foreach (var sessionItem in sessionCart)
                {
                    var dbItem = dbCartItems.FirstOrDefault(ci => ci.ProductID == sessionItem.Product.ProductID);
                    if (dbItem != null)
                    {
                        // Sản phẩm đã có trong DB, cộng thêm số lượng từ session
                        dbItem.Quantity += sessionItem.Quantity;
                    }
                    else
                    {
                        // Sản phẩm chưa có trong DB, thêm mới
                        CartItem newItem = new CartItem // Lớp CartItem từ LINQ to SQL
                        {
                            UserID = userId,
                            ProductID = sessionItem.Product.ProductID,
                            Quantity = sessionItem.Quantity,
                            DateAdded = DateTime.Now
                        };
                        _db.CartItems.InsertOnSubmit(newItem); // THAY THẾ CartItems (Table<T>)
                    }
                }

                try
                {
                    _db.SubmitChanges();
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi hợp nhất giỏ hàng
                    System.Diagnostics.Debug.WriteLine("Lỗi khi hợp nhất giỏ hàng: " + ex.Message);
                }
            }

            // Xóa giỏ hàng trong session sau khi đã hợp nhất
            Session["Cart"] = null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOut() // Đổi tên thành LogOut để khớp với các View của bạn
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }


        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
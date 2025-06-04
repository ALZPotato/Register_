using MyRegistrationApp.Models; 
using System;
using System.Linq;
using System.Security.Cryptography; 
using System.Text;
using System.Web.Mvc;
using System.Configuration;

namespace MyRegistrationApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account/Register
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = ConfigurationManager.ConnectionStrings["MyWebAppDBConnectionString"].ConnectionString;
                using (UserDataDataContext db = new UserDataDataContext(connectionString)) 
                {
                    // 1. Kiểm tra xem Username đã tồn tại chưa
                    var existingUser = db.Users.FirstOrDefault(u => u.Username == model.Username);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
                        return View(model);
                    }

                    // 2. Băm mật khẩu (SHA256)
                    string hashedPassword = HashPassword(model.Password);

                    // 3. Tạo đối tượng User mới (từ lớp User đã được LINQ to SQL sinh ra)
                    User newUser = new User 
                    {
                        Username = model.Username,
                        PasswordHash = hashedPassword,
                        FullName = model.FullName,
                        Business = model.Business, 
                        RegisteredDate = DateTime.Now
                    };

                    // 4. Thêm user mới vào context và lưu thay đổi
                    db.Users.InsertOnSubmit(newUser);
                    try
                    {
                        db.SubmitChanges();
                        // Đăng ký thành công, có thể chuyển hướng đến trang đăng nhập hoặc trang chủ
                        ViewBag.Message = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
                        // return RedirectToAction("Login", "Account");
                        return View("RegistrationSuccess");
                    }
                    catch (Exception ex)
                    {
                        // Ghi log lỗi 
                        // Log.Error("Lỗi khi đăng ký user: " + ex.Message);
                        ModelState.AddModelError("", "Đã có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại.");
                    }
                }
            }
            return View(model);
        }

        // Hàm băm mật khẩu
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Chuyển đổi chuỗi đầu vào thành mảng byte và tính toán hash.
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Chuyển đổi mảng byte thành chuỗi hex.
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
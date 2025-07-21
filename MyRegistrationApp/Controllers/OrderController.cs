using MyRegistrationApp.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Security;


namespace MyRegistrationApp.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private UserDataDataContext _db;

        public OrderController()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyWebAppDBConnectionString"].ConnectionString;
            _db = new UserDataDataContext(connectionString); 
        }

        // GET: /Order/Checkout
        public ActionResult Checkout()
        {
            var user = _db.Users.SingleOrDefault(u => u.Username == User.Identity.Name); 
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItemsFromDb = _db.CartItems.Where(c => c.UserID == user.UserID).ToList(); 

            if (!cartItemsFromDb.Any())
            {
                TempData["Message"] = "Giỏ hàng của bạn đang trống. Hãy thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            var orderViewModel = new OrderViewModel
            {
                FullName = user.FullName,
                Email = user.Email, 
                Cart = new CartViewModel
                {
                    CartItems = cartItemsFromDb.Select(dbItem => new MyRegistrationApp.Models.CartItem 
                    {
                        Product = dbItem.Product, 
                        Quantity = dbItem.Quantity
                    }).ToList()
                }
            };

            ViewBag.Title = "Thông Tin Đặt Hàng";
            return View(orderViewModel);
        }

        // POST: /Order/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(OrderViewModel model)
        {
            var user = _db.Users.SingleOrDefault(u => u.Username == User.Identity.Name); 
            var cartItems = _db.CartItems.Where(c => c.UserID == user.UserID).ToList(); 

            if (!ModelState.IsValid)
            {
                // Nếu form không hợp lệ, load lại giỏ hàng và hiển thị lại trang Checkout
                model.Cart = new CartViewModel
                {
                    CartItems = cartItems.Select(ci => new MyRegistrationApp.Models.CartItem { Product = ci.Product, Quantity = ci.Quantity }).ToList()
                };
                ViewBag.Title = "Thông Tin Đặt Hàng";
                return View("Checkout", model);
            }

            if (!cartItems.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn trống, không thể đặt hàng.");
                model.Cart = new CartViewModel { CartItems = new System.Collections.Generic.List<MyRegistrationApp.Models.CartItem>() };
                return View("Checkout", model);
            }
            string orderStatus;
            if (model.PaymentMethod == "VietQR")
            {
                orderStatus = "Chờ thanh toán";
            }
            else // Mặc định là COD
            {
                orderStatus = "Đang xử lý";
            }
            // Bắt đầu tạo đơn hàng
            Order newOrder = new Order 
            {
                UserID = user.UserID,
                OrderDate = DateTime.Now,
                RecipientName = model.FullName,
                ShippingAddress = model.Address,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Notes = model.Notes,
                Status = orderStatus,
                TotalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity)
            };

            _db.Orders.InsertOnSubmit(newOrder); 
            _db.SubmitChanges(); // Lưu để lấy OrderID

            // Thêm các sản phẩm vào bảng OrderDetails
            foreach (var item in cartItems)
            {
                OrderDetail detail = new OrderDetail 
                {
                    OrderID = newOrder.OrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                };
                _db.OrderDetails.InsertOnSubmit(detail); 

                // Trừ số lượng tồn kho
                var productInDb = _db.Products.SingleOrDefault(p => p.ProductID == item.ProductID);
                if (productInDb != null)
                {
                    productInDb.StockQuantity -= item.Quantity;
                }
            }

            // Xóa giỏ hàng sau khi đã chuyển sang đơn hàng
            _db.CartItems.DeleteAllOnSubmit(cartItems);

            // Lưu tất cả thay đổi
            _db.SubmitChanges();

            // Chuyển hướng đến trang hướng dẫn thanh toán
            if (model.PaymentMethod == "VietQR")
            {
                return RedirectToAction("PaymentInstructions", new { id = newOrder.OrderID });
            }
            else // Mặc định là COD
            {
                return RedirectToAction("OrderSuccess", new { id = newOrder.OrderID });
            }
        }

        // GET: /Order/PaymentInstructions/5
        public ActionResult PaymentInstructions(int id)
        {
            var user = _db.Users.SingleOrDefault(u => u.Username == User.Identity.Name); 
            if (user == null) return RedirectToAction("Login", "Account");

            var order = _db.Orders.FirstOrDefault(o => o.OrderID == id && o.UserID == user.UserID); 
            if (order == null)
            {
                return HttpNotFound();
            }

            // --- TẠO THÔNG TIN VIETQR ---
            string bankId = ConfigurationManager.AppSettings["VietQR_BankId"];
            string accountNo = ConfigurationManager.AppSettings["VietQR_AccountNo"];
            string accountName = ConfigurationManager.AppSettings["VietQR_AccountName"];
            string orderAmount = ((long)order.TotalAmount).ToString();
            string orderInfo = "DH" + order.OrderID; 
            string template = "compact2";

            if (string.IsNullOrEmpty(bankId) || string.IsNullOrEmpty(accountNo) || string.IsNullOrEmpty(accountName))
            {
                // Xử lý lỗi nếu thiếu cấu hình trong Web.config
                ViewBag.QrError = "Lỗi cấu hình thanh toán. Vui lòng liên hệ quản trị viên.";
            }
            else
            {
                // Tạo URL gọi đến API sinh mã QR
                ViewBag.VietQrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={orderAmount}&addInfo={Uri.EscapeDataString(orderInfo)}&accountName={Uri.EscapeDataString(accountName)}";
            }

            ViewBag.OrderAmount = order.TotalAmount;
            ViewBag.OrderInfo = orderInfo;
            ViewBag.BankAccountName = accountName;
            ViewBag.BankAccountNo = accountNo;
            ViewBag.BankName = "[Tên Ngân Hàng Của Bạn]"; // Điền tay tên NH cho đẹp
            // -----------------------------

            ViewBag.Title = "Hướng Dẫn Thanh Toán VietQR";
            return View(order);
        }
        // GET: /Order/OrderSuccess/5
        public ActionResult OrderSuccess(int id)
        {
            var user = _db.Users.SingleOrDefault(u => u.Username == User.Identity.Name); 
            if (user == null) return RedirectToAction("Login", "Account");

            var order = _db.Orders.FirstOrDefault(o => o.OrderID == id && o.UserID == user.UserID); 
            if (order == null)
            {
                return HttpNotFound();
            }
            ViewBag.Title = "Đặt Hàng Thành Công!";
            return View(order);
        }
        // GET: /Order/History
        public ActionResult History()
        {
            ViewBag.Title = "Lịch Sử Đơn Hàng";

            var user = _db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                // Đăng xuất nếu user không tồn tại trong DB dù đã xác thực
                FormsAuthentication.SignOut();
                return RedirectToAction("Login", "Account");
            }
            var orders = _db.Orders.Where(o => o.UserID == user.UserID) 
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

            return View(orders); 
        }

        // GET: /Order/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = _db.Users.SingleOrDefault(u => u.Username == User.Identity.Name); 
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 1. Tìm đơn hàng gốc trong DB
            var orderFromDb = _db.Orders.FirstOrDefault(o => o.OrderID == id && o.UserID == user.UserID);

            if (orderFromDb == null)
            {
                return HttpNotFound();
            }

            // 2. Tạo đối tượng ViewModel để gửi đi
            var viewModel = new UserOrderViewModel
            {
                OrderID = orderFromDb.OrderID,
                OrderDate = orderFromDb.OrderDate,
                Status = orderFromDb.Status,
                TotalAmount = orderFromDb.TotalAmount,
                RecipientName = orderFromDb.RecipientName,
                ShippingAddress = orderFromDb.ShippingAddress,
                PhoneNumber = orderFromDb.PhoneNumber,
                Email = orderFromDb.Email, 
                Notes = orderFromDb.Notes,

                // 3. Chuyển đổi EntitySet<OrderDetail> thành List<UserOrderDetailViewModel>
                // BƯỚC NÀY ĐƯỢC THỰC HIỆN TRONG CONTROLLER
                Items = orderFromDb.OrderDetails.Select(detail => new UserOrderDetailViewModel
                {
                    ProductID = detail.ProductID,
                    ProductName = detail.Product?.ProductName, 
                    ProductImageFileName = detail.Product?.ImageFileName,
                    Quantity = detail.Quantity,
                    Price = detail.Price
                }).ToList() // .ToList() sẽ thực thi và tạo ra một List mới
            };

            ViewBag.Title = string.Format("Chi Tiết Đơn Hàng #{0}", viewModel.OrderID);

            // 4. Trả về ViewModel đã được "làm sạch", không còn EntitySet<>
            return View(viewModel);
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
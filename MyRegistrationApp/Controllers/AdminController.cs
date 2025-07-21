
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using MyRegistrationApp.Models;
using System;
using System.IO;                    
using System.Net;               


namespace MyRegistrationApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private UserDataDataContext _db;
        // GET: Admin (Trang chủ của admin)
        public AdminController()
        {
            // Khởi tạo DataContext trong constructor
            string connectionString = ConfigurationManager.ConnectionStrings["MyWebAppDBConnectionString"].ConnectionString;
            _db = new UserDataDataContext(connectionString);
        }
        public ActionResult Index()
        {
            ViewBag.Title = "Bảng Điều Khiển Admin";
            return View();
        }

        // GET: Admin/Feedbacks
        public ActionResult Feedbacks(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewBag.Title = "Quản Lý Phản Hồi";
            ViewBag.CurrentFilter = searchString;
            IQueryable<CustomerFeedback> feedbacksQuery = _db.CustomerFeedbacks; 
            if (!String.IsNullOrEmpty(searchString))
            {
                feedbacksQuery = feedbacksQuery.Where(s => (s.Subject != null && s.Subject.Contains(searchString)) ||
                                                           (s.Message != null && s.Message.Contains(searchString)) ||
                                                           (s.FullName != null && s.FullName.Contains(searchString)));
            }
            feedbacksQuery = feedbacksQuery.OrderByDescending(s => s.SubmittedDate);
            // Thêm logic phân trang nếu muốn
            var feedbacks = feedbacksQuery.ToList();
            return View(feedbacks);
        }
        // POST: Admin/MarkAsResolved 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsResolved(int feedbackId, bool isResolved)
        {
            var feedback = _db.CustomerFeedbacks.SingleOrDefault(f => f.FeedbackID == feedbackId); 
            if (feedback != null)
            {
                feedback.IsResolved = isResolved;
                _db.SubmitChanges();
                TempData["AdminMessage"] = "Cập nhật trạng thái phản hồi thành công.";
            }
            return RedirectToAction("Feedbacks");
        }

        public ActionResult Products(string searchString)
        {
            ViewBag.Title = "Quản Lý Sản Phẩm";
            ViewBag.CurrentFilter = searchString;

            IQueryable<Product> productsQuery = _db.Products; 

            if (!String.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p =>
                    (p.ProductName != null && p.ProductName.Contains(searchString)) ||
                    (p.Category != null && p.Category.CategoryName != null && p.Category.CategoryName.Contains(searchString))
                );
            }

            var products = productsQuery.OrderByDescending(p => p.DateCreated).ToList();
            return View(products);
        }

        private List<SelectListItem> GetCategoryOptions(int? selectedCategoryId = null)
        {
            var categoriesFromDb = _db.Categories 
                                      .OrderBy(c => c.CategoryName)
                                      .Select(c => new // Tạo một anonymous type hoặc một DTO đơn giản trước
                                      {
                                          c.CategoryID, // Đảm bảo CategoryID không null trong DB nếu nó là khóa chính
                                          c.CategoryName
                                      })
                                      .ToList(); // Lấy dữ liệu từ DB trước

            var categoryOptions = categoriesFromDb.Select(c => new SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.CategoryName,
                // Gán Selected ở đây, sau khi đã có dữ liệu trong bộ nhớ
                Selected = (selectedCategoryId.HasValue && c.CategoryID == selectedCategoryId.Value)
            }).ToList();

            return categoryOptions;
        }

        // GET: Admin/CreateProduct
        public ActionResult CreateProduct()
        {
            ViewBag.Title = "Thêm Sản Phẩm Mới";
            var viewModel = new ProductViewModel
            {
                CategoryOptions = GetCategoryOptions(),
                IsActive = true
            };
            return View(viewModel);
        }

        // POST: Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult CreateProduct(ProductViewModel model)
        {
            model.CategoryOptions = GetCategoryOptions(model.CategoryID);
            ViewBag.Title = "Thêm Sản Phẩm Mới";
            string imageFileNameToSave = null;

            if (model.ProductImageFile != null && model.ProductImageFile.ContentLength > 0)
            {
                if (model.ProductImageFile.ContentLength > 5 * 1024 * 1024) { ModelState.AddModelError("ProductImageFile", "Kích thước ảnh không được vượt quá 5MB."); }
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(model.ProductImageFile.FileName)?.ToLower();
                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension)) { ModelState.AddModelError("ProductImageFile", "Chỉ chấp nhận file ảnh có định dạng: jpg, png, gif."); }

                if (ModelState.IsValidField("ProductImageFile"))
                {
                    try
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        string uploadFolderPath = Server.MapPath("~/Uploads/ProductImages/");
                        if (!Directory.Exists(uploadFolderPath)) { Directory.CreateDirectory(uploadFolderPath); }
                        string filePath = Path.Combine(uploadFolderPath, uniqueFileName);
                        model.ProductImageFile.SaveAs(filePath);
                        imageFileNameToSave = uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ProductImageFile", "Lỗi khi tải lên ảnh: " + ex.Message);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                Product newProduct = new Product() 
                {
                    ProductName = model.ProductName,
                    Description = model.Description,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    ImageFileName = imageFileNameToSave,
                    CategoryID = model.CategoryID,
                    IsActive = model.IsActive,
                    DateCreated = DateTime.Now
                };
                _db.Products.InsertOnSubmit(newProduct);
                try
                {
                    _db.SubmitChanges();
                    TempData["AdminMessage"] = "Đã thêm sản phẩm '" + newProduct.ProductName + "' thành công!";
                    return RedirectToAction("Products");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu sản phẩm: " + ex.Message);
                }
            }
            return View(model);
        }

        // GET: Admin/EditProduct/5
        public ActionResult EditProduct(int? id)
        {
            if (id == null) { return new HttpStatusCodeResult(HttpStatusCode.BadRequest); }
            Product product = _db.Products.FirstOrDefault(p => p.ProductID == id);
            if (product == null) { return HttpNotFound(); }

            ViewBag.Title = "Chỉnh Sửa Sản Phẩm: " + product.ProductName;
            var viewModel = new ProductViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CurrentImageFileName = product.ImageFileName,
                CategoryID = product.CategoryID,
                IsActive = product.IsActive,
                CategoryOptions = GetCategoryOptions(product.CategoryID)
            };
            return View(viewModel);
        }

        // POST: Admin/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult EditProduct(ProductViewModel model)
        {
            model.CategoryOptions = GetCategoryOptions(model.CategoryID);
            ViewBag.Title = "Chỉnh Sửa Sản Phẩm";
            string imageFileNameToUpdate = model.CurrentImageFileName;

            if (model.ProductImageFile != null && model.ProductImageFile.ContentLength > 0)
            {
                if (model.ProductImageFile.ContentLength > 5 * 1024 * 1024) { ModelState.AddModelError("ProductImageFile", "Ảnh không quá 5MB."); }
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(model.ProductImageFile.FileName)?.ToLower();
                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension)) { ModelState.AddModelError("ProductImageFile", "Chỉ chấp nhận file ảnh: jpg, png, gif."); }


                if (ModelState.IsValidField("ProductImageFile"))
                {
                    try
                    {
                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(model.CurrentImageFileName))
                        {
                            string oldFilePath = Path.Combine(Server.MapPath("~/Uploads/ProductImages/"), model.CurrentImageFileName);
                            if (System.IO.File.Exists(oldFilePath)) { System.IO.File.Delete(oldFilePath); }
                        }
                        // Lưu ảnh mới
                        string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        string uploadFolderPath = Server.MapPath("~/Uploads/ProductImages/");
                        if (!Directory.Exists(uploadFolderPath)) { Directory.CreateDirectory(uploadFolderPath); }
                        string newFilePath = Path.Combine(uploadFolderPath, uniqueFileName);
                        model.ProductImageFile.SaveAs(newFilePath);
                        imageFileNameToUpdate = uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ProductImageFile", "Lỗi khi tải lên ảnh mới: " + ex.Message);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                Product productToUpdate = _db.Products.FirstOrDefault(p => p.ProductID == model.ProductID);
                if (productToUpdate == null) { return HttpNotFound(); }

                productToUpdate.ProductName = model.ProductName;
                productToUpdate.Description = model.Description;
                productToUpdate.Price = model.Price;
                productToUpdate.StockQuantity = model.StockQuantity;
                productToUpdate.ImageFileName = imageFileNameToUpdate;
                productToUpdate.CategoryID = model.CategoryID;
                productToUpdate.IsActive = model.IsActive;
                productToUpdate.DateModified = DateTime.Now;

                try
                {
                    _db.SubmitChanges();
                    TempData["AdminMessage"] = "Đã cập nhật sản phẩm '" + productToUpdate.ProductName + "' thành công!";
                    return RedirectToAction("Products");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật sản phẩm: " + ex.Message);
                }
            }
            return View(model);
        }

        // GET: Admin/DeleteProduct/5
        public ActionResult DeleteProduct(int? id)
        {
            if (id == null) { return new HttpStatusCodeResult(HttpStatusCode.BadRequest); }
            Product product = _db.Products.FirstOrDefault(p => p.ProductID == id); 
            if (product == null) { return HttpNotFound(); }
            ViewBag.Title = "Xác Nhận Xóa Sản Phẩm";
            return View(product);
        }

        // POST: Admin/DeleteProduct/5
        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProductConfirmed(int id)
        {
            Product productToDelete = _db.Products.FirstOrDefault(p => p.ProductID == id);
            if (productToDelete == null) { return HttpNotFound(); }

            string imagePathToDelete = null;
            if (!string.IsNullOrEmpty(productToDelete.ImageFileName))
            {
                imagePathToDelete = Path.Combine(Server.MapPath("~/Uploads/ProductImages/"), productToDelete.ImageFileName);
            }
            try
            {
                _db.Products.DeleteOnSubmit(productToDelete); 
                _db.SubmitChanges();

                if (imagePathToDelete != null && System.IO.File.Exists(imagePathToDelete))
                {
                    System.IO.File.Delete(imagePathToDelete);
                }
                TempData["AdminMessage"] = "Đã xóa sản phẩm '" + productToDelete.ProductName + "' thành công!";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Lỗi khi xóa sản phẩm: " + ex.Message;
            }
            return RedirectToAction("Products");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
        // GET: Admin/Orders
        public ActionResult Orders(string searchString, string statusFilter)
        {
            ViewBag.Title = "Quản Lý Đơn Hàng";
            ViewBag.CurrentFilter = searchString;
            ViewBag.StatusFilter = statusFilter;

            IQueryable<Order> ordersQuery = _db.Orders.OrderByDescending(o => o.OrderDate); // Sắp xếp đơn hàng mới nhất lên đầu 

            // Lọc theo từ khóa tìm kiếm (mã đơn hàng, tên người nhận, sđt)
            if (!String.IsNullOrEmpty(searchString))
            {
                // Chuyển đổi searchString thành số nếu có thể để tìm theo OrderID
                int searchId = 0;
                bool isNumeric = int.TryParse(searchString, out searchId);

                ordersQuery = ordersQuery.Where(o =>
                    (o.RecipientName != null && o.RecipientName.Contains(searchString)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.Contains(searchString)) ||
                    (isNumeric && o.OrderID == searchId)
                );
            }

            // Lọc theo trạng thái đơn hàng
            if (!string.IsNullOrEmpty(statusFilter))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == statusFilter);
            }

            var orders = ordersQuery.ToList(); // Lấy danh sách cuối cùng

            // Tạo danh sách các trạng thái để hiển thị trong dropdown filter
            ViewBag.StatusOptions = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Tất cả trạng thái" },
            new SelectListItem { Value = "Đang xử lý", Text = "Đang xử lý" },
            new SelectListItem { Value = "Chờ thanh toán", Text = "Chờ thanh toán" },
            new SelectListItem { Value = "Đang giao hàng", Text = "Đang giao hàng" },
            new SelectListItem { Value = "Đã hoàn thành", Text = "Đã hoàn thành" },
            new SelectListItem { Value = "Đã hủy", Text = "Đã hủy" }
        };


            return View(orders); 
        }
        public ActionResult OrderDetail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 1. Tìm đơn hàng trong DB
            Order orderFromDb = _db.Orders.FirstOrDefault(o => o.OrderID == id);

            if (orderFromDb == null)
            {
                return HttpNotFound();
            }

            // 2. Tạo một ViewModel mới
            var viewModel = new AdminOrderDetailViewModel
            {
                // Gán thông tin chung của đơn hàng
                OrderID = orderFromDb.OrderID,
                OrderDate = orderFromDb.OrderDate,
                CurrentStatus = orderFromDb.Status,
                TotalAmount = orderFromDb.TotalAmount,

                // Gán thông tin khách hàng (kiểm tra null để an toàn)
                CustomerFullName = orderFromDb.User?.FullName, 
                CustomerUsername = orderFromDb.User?.Username,

                // Gán thông tin giao hàng
                RecipientName = orderFromDb.RecipientName,
                ShippingAddress = orderFromDb.ShippingAddress,
                PhoneNumber = orderFromDb.PhoneNumber,
                Email = orderFromDb.Email,
                Notes = orderFromDb.Notes,

                // 3. Chuyển đổi danh sách OrderDetails (LINQ to SQL) sang List<OrderDetailViewModel>
                Items = orderFromDb.OrderDetails.Select(detail => new OrderDetailViewModel
                {
                    ProductID = detail.ProductID,
                    ProductName = detail.Product?.ProductName, 
                    Quantity = detail.Quantity,
                    Price = detail.Price
                }).ToList(),

                // 4. Chuẩn bị danh sách trạng thái cho DropDownList
                StatusOptions = new List<SelectListItem>
        {
            new SelectListItem { Text = "Đang xử lý", Value = "Đang xử lý" },
            new SelectListItem { Text = "Chờ thanh toán", Value = "Chờ thanh toán" },
            new SelectListItem { Text = "Đang giao hàng", Value = "Đang giao hàng" },
            new SelectListItem { Text = "Đã hoàn thành", Value = "Đã hoàn thành" },
            new SelectListItem { Text = "Đã hủy", Value = "Đã hủy" }
        }
            };

            ViewBag.Title = $"Chi Tiết Đơn Hàng #{viewModel.OrderID}";

            // 5. Trả về ViewModel cho View
            return View(viewModel);
        }

        // Action UpdateOrderStatus không cần thay đổi nhiều, nó vẫn nhận orderId và status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(int orderId, string status)
        {
            Order orderToUpdate = _db.Orders.FirstOrDefault(o => o.OrderID == orderId);
                                                                                       
            orderToUpdate.Status = status;
            _db.SubmitChanges();
            return RedirectToAction("OrderDetail", new { id = orderId });
        }
    }
}
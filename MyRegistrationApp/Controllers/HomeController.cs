using MyRegistrationApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration; 
using System.IO;          
using System.Linq; 
using System.Net;         
using System.Net.Mail;    
using System.Text;        
using System.Web.Mvc;


namespace MyRegistrationApp.Controllers
{
    public class HomeController : Controller
    {
        private UserDataDataContext _db;

        public HomeController()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyWebAppDBConnectionString"].ConnectionString;
            _db = new UserDataDataContext(connectionString);
        }
        public ActionResult Index()
        {
            var viewModel = new HomeViewModel();

            viewModel.FeaturedProducts = _db.Products 
                                            .Where(p => p.IsActive)
                                            .OrderByDescending(p => p.DateCreated)
                                            .Take(3)
                                            .ToList();

            ViewBag.PageTitle = "Trang Chủ - The Coffee";
            ViewBag.HeroTitle = "Khám Phá Hương Vị Cà Phê Đích Thực";

            return View(viewModel); 
        }
        public ActionResult About() // Trang giới thiệu về The Coffee
        {
            ViewBag.Title = "Về The Coffee";
            ViewBag.Message = "Tìm hiểu thêm về câu chuyện và sứ mệnh của chúng tôi.";
            return View(); 
        }

        // GET: Home/Contact
        public ActionResult Contact()
        {
            ViewBag.Message = "Trang liên hệ của bạn.";
            return View();
        }
        public ActionResult Products(string categoryName = null)
        {
            var viewModel = new ProductsViewModel();
            string connectionString = ConfigurationManager.ConnectionStrings["MyWebAppDBConnectionString"].ConnectionString;

            using (UserDataDataContext db = new UserDataDataContext(connectionString))
            {
                // 1. Lấy tất cả danh mục để hiển thị menu
                viewModel.Categories = db.Categories.OrderBy(c => c.CategoryName).ToList(); 

                // 2. Bắt đầu truy vấn lấy sản phẩm, chỉ lấy sản phẩm đang kích hoạt
                IQueryable<Product> productsQuery = db.Products.Where(p => p.IsActive); 

                if (!string.IsNullOrEmpty(categoryName))
                {
                    // Người dùng đã chọn một danh mục cụ thể
                    var selectedCategory = viewModel.Categories.FirstOrDefault(c =>
                        c.CategoryName != null &&
                        c.CategoryName.Replace(" ", "-").Equals(categoryName, StringComparison.OrdinalIgnoreCase)
                    );

                    if (selectedCategory != null)
                    {
                        // Lọc sản phẩm theo CategoryID của danh mục đã chọn
                        productsQuery = productsQuery.Where(p => p.CategoryID == selectedCategory.CategoryID);
                        viewModel.CurrentCategoryName = selectedCategory.CategoryName;
                        ViewBag.Title = $"Sản Phẩm - {selectedCategory.CategoryName}";
                    }
                    else
                    {
                        // Danh mục không hợp lệ hoặc không tìm thấy
                        ViewBag.Title = "Sản Phẩm - Danh mục không tồn tại";
                        viewModel.CurrentCategoryName = "Danh mục không tồn tại";
                        viewModel.ProductsToList = new List<Product>(); // Trả về danh sách rỗng để không hiển thị sản phẩm nào
                        return View(viewModel); // Trả về view với danh sách sản phẩm rỗng
                    }
                }
                else
                {
                    // Không có categoryName được chỉ định (người dùng muốn xem TẤT CẢ SẢN PHẨM)
                    // Không cần áp dụng thêm bộ lọc nào vào productsQuery
                    ViewBag.Title = "Tất Cả Sản Phẩm";
                    viewModel.CurrentCategoryName = "Tất Cả Sản Phẩm";
                }

                // Sắp xếp sản phẩm (ví dụ: theo ngày tạo mới nhất) và lấy danh sách cuối cùng
                viewModel.ProductsToList = productsQuery.OrderByDescending(p => p.DateCreated).ToList();
            }

            return View(viewModel);
        }

        public ActionResult ContactFeedback()
        {
            var model = new FeedbackViewModel();
            ViewBag.Title = "Gửi Phản Hồi Cho The Coffee";
            return View(model);
        }

        // POST: Home/ContactFeedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ContactFeedback(FeedbackViewModel model)
        {
            ViewBag.Title = "Gửi Phản Hồi Cho The Coffee";
            string uploadedFileName = null;
            string fullPathToUploadedFile = null;

            // --- VALIDATE FILE UPLOAD ---
            if (model.AttachedImageFile != null && model.AttachedImageFile.ContentLength > 0)
            {
                if (model.AttachedImageFile.ContentLength > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError("AttachedImageFile", "Kích thước ảnh không được vượt quá 5MB.");
                }
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(model.AttachedImageFile.FileName)?.ToLower();
                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("AttachedImageFile", "Chỉ chấp nhận file ảnh có định dạng: jpg, png, gif.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // --- XỬ LÝ LƯU FILE ẢNH ---
                if (model.AttachedImageFile != null && model.AttachedImageFile.ContentLength > 0)
                {
                    try
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AttachedImageFile.FileName);
                        string uploadFolderPath = Server.MapPath("~/Uploads/FeedbackImages/");

                        if (!Directory.Exists(uploadFolderPath))
                        {
                            Directory.CreateDirectory(uploadFolderPath);
                        }

                        fullPathToUploadedFile = Path.Combine(uploadFolderPath, uniqueFileName);
                        model.AttachedImageFile.SaveAs(fullPathToUploadedFile);
                        uploadedFileName = uniqueFileName;
                        System.Diagnostics.Debug.WriteLine($"File saved to: {fullPathToUploadedFile}");
                    }
                    catch (Exception exSaveFile)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving file: {exSaveFile.ToString()}");
                        ModelState.AddModelError("AttachedImageFile", "Có lỗi xảy ra khi tải lên ảnh của bạn. Chi tiết: " + exSaveFile.Message);
                    }
                }

                // Kiểm tra ModelState một lần nữa trước khi lưu vào DB,
                // vì lỗi lưu file có thể đã được thêm vào ModelState
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // --- LƯU VÀO DATABASE ---
                string connectionString = ConfigurationManager.ConnectionStrings["MyWebAppDBConnectionString"].ConnectionString;
                using (UserDataDataContext db = new UserDataDataContext(connectionString))
                {
                    CustomerFeedback newFeedback = new CustomerFeedback
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        FeedbackType = model.FeedbackType,
                        Subject = model.Subject,
                        Message = model.Message,
                        AllowContact = model.AllowContact,
                        SubmittedDate = DateTime.Now,
                        IsResolved = false,
                        AttachedImageFileName = uploadedFileName
                    };
                    db.CustomerFeedbacks.InsertOnSubmit(newFeedback);
                    db.SubmitChanges();
                    System.Diagnostics.Debug.WriteLine($"Feedback saved to DB. ImageFileName: {uploadedFileName}");
                }


                // --- GỬI EMAIL THÔNG BÁO ---
                try
                {
                    string smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
                    int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                    string smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"];
                    string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
                    bool enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true");
                    string feedbackRecipientEmail = ConfigurationManager.AppSettings["FeedbackRecipientEmail"];
                    string emailSenderAddress = ConfigurationManager.AppSettings["EmailSenderAddress"] ?? smtpUsername;

                    if (!string.IsNullOrEmpty(smtpHost) && !string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword) && !string.IsNullOrEmpty(feedbackRecipientEmail))
                    {
                        MailMessage mail = new MailMessage();
                        mail.To.Add(new MailAddress(feedbackRecipientEmail));
                        mail.From = new MailAddress(emailSenderAddress, "The Coffee - Hệ Thống Phản Hồi");
                        mail.Subject = $"[The Coffee] Phản hồi mới: {model.Subject} ({model.FeedbackType})";

                        StringBuilder body = new StringBuilder();
                        body.AppendLine("<html><body>");
                        body.AppendLine($"<h2>Bạn đã nhận được một phản hồi mới từ website The Coffee:</h2>");
                        body.AppendLine($"<p><strong>Người gửi:</strong> {System.Web.HttpUtility.HtmlEncode(model.FullName)}</p>"); // Encode để tránh XSS
                        body.AppendLine($"<p><strong>Email:</strong> {System.Web.HttpUtility.HtmlEncode(model.Email)}</p>");
                        if (!string.IsNullOrEmpty(model.PhoneNumber))
                        {
                            body.AppendLine($"<p><strong>Số điện thoại:</strong> {System.Web.HttpUtility.HtmlEncode(model.PhoneNumber)}</p>");
                        }
                        body.AppendLine($"<p><strong>Loại phản hồi:</strong> {System.Web.HttpUtility.HtmlEncode(model.FeedbackType)}</p>");
                        body.AppendLine($"<p><strong>Tiêu đề:</strong> {System.Web.HttpUtility.HtmlEncode(model.Subject)}</p>");
                        body.AppendLine($"<p><strong>Nội dung:</strong></p>");
                        body.AppendLine($"<div style='border:1px solid #eee; padding:10px; background-color:#f9f9f9;'>{System.Web.HttpUtility.HtmlEncode(model.Message).Replace(Environment.NewLine, "<br/>")}</div>");
                        body.AppendLine($"<p><strong>Cho phép liên hệ lại:</strong> {(model.AllowContact ? "Có" : "Không")}</p>");
                        body.AppendLine($"<p><strong>Thời gian gửi:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");

                        if (!string.IsNullOrEmpty(uploadedFileName))
                        {
                            Uri baseUri = Request.Url; // Lấy Url của request hiện tại
                            string siteBaseUrl = $"{baseUri.Scheme}://{baseUri.Authority}{Url.Content("~")}";
                            string imageUrl = siteBaseUrl.TrimEnd('/') + "/Uploads/FeedbackImages/" + Uri.EscapeDataString(uploadedFileName); // Đảm bảo URL hợp lệ

                            body.AppendLine($"<p><strong>Ảnh đính kèm:</strong> <a href='{imageUrl}'>{System.Web.HttpUtility.HtmlEncode(uploadedFileName)}</a></p>");
                        }
                        body.AppendLine("</body></html>");

                        mail.Body = body.ToString();
                        mail.IsBodyHtml = true;

                        SmtpClient smtp = new SmtpClient(smtpHost, smtpPort);
                        smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                        smtp.EnableSsl = enableSsl;

                        smtp.Send(mail);
                        System.Diagnostics.Debug.WriteLine("Feedback email sent.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("SMTP configuration is missing. Email not sent.");
                    }
                }
                catch (SmtpException smtpEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SMTP Error sending email: {smtpEx.ToString()}");
                    ModelState.AddModelError("EmailSendError", "Phản hồi đã được ghi nhận nhưng không thể gửi email thông báo. Mã lỗi SMTP: " + smtpEx.StatusCode);
                }
                catch (Exception emailEx)
                {
                    System.Diagnostics.Debug.WriteLine($"General Error sending email: {emailEx.ToString()}");
                    ModelState.AddModelError("EmailSendError", "Phản hồi đã được ghi nhận nhưng có lỗi xảy ra trong quá trình gửi email thông báo.");
                }
                // --- KẾT THÚC GỬI EMAIL ---

                TempData["FeedbackSuccessMessage"] = "Cảm ơn bạn đã gửi phản hồi! Chúng tôi sẽ xem xét và liên hệ lại nếu cần.";
                return RedirectToAction("ContactFeedback");
            }
            catch (Exception ex) // Lỗi chung không lường trước (ví dụ: lỗi DB, lỗi logic khác)
            {
                System.Diagnostics.Debug.WriteLine($"General Error processing feedback: {ex.ToString()}");
                ModelState.AddModelError("", "Đã có lỗi không mong muốn xảy ra trong quá trình gửi phản hồi. Vui lòng thử lại sau. Chi tiết: " + ex.Message);
                return View(model);
            }
        }
        // GET: Home/ProductDetail/5
        public ActionResult ProductDetail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Yêu cầu ID sản phẩm.");
            }

            Product product = _db.Products.FirstOrDefault(p => p.ProductID == id && p.IsActive); 

            if (product == null)
            {
                return HttpNotFound("Không tìm thấy sản phẩm này hoặc sản phẩm đã ngừng kinh doanh.");
            }

            ViewBag.Title = product.ProductName; // Đặt tiêu đề trang theo tên sản phẩm
            return View(product); // Truyền đối tượng Product (LINQ to SQL) trực tiếp vào View
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
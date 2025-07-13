// Global.asax.cs
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Security.Principal;

namespace MyRegistrationApp // Thay bằng namespace của bạn
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // THÊM PHƯƠNG THỨC NÀY
        protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
        {
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                try
                {
                    FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                    if (authTicket != null && !authTicket.Expired)
                    {
                        string[] roles = authTicket.UserData.Split(','); // UserData chứa vai trò
                        IIdentity identity = new FormsIdentity(authTicket);
                        IPrincipal principal = new GenericPrincipal(identity, roles);
                        HttpContext.Current.User = principal;
                    }
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi giải mã ticket hoặc xử lý lỗi khác
                    System.Diagnostics.Debug.WriteLine("Lỗi giải mã authentication ticket: " + ex.Message);
                }
            }
        }
    }
}
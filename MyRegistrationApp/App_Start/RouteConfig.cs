using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MyRegistrationApp
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Route cho trang sản phẩm với tên danh mục
            routes.MapRoute(
                name: "ProductsByCategory",
                url: "Products/{categoryName}", // URL sẽ là /Products/Ten-Category
                defaults: new { controller = "Home", action = "Products", categoryName = UrlParameter.Optional }
            );

            // Route cho trang sản phẩm không có category (hiển thị tất cả)
            routes.MapRoute(
                name: "ProductsAll",
                url: "Products",
                defaults: new { controller = "Home", action = "Products" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

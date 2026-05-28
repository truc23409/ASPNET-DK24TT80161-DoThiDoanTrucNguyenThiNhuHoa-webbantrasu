using System;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;
using System.Collections.Generic;

namespace WebBanHang.Controllers
{
    public class HomeAdminController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                filterContext.Result = RedirectToAction("Login", "Login", new { area = "" });
            }
            base.OnActionExecuting(filterContext);
        }

        public ActionResult HomeAdmin()
        {
            // 1. Tổng số sản phẩm & nguyên liệu
            ViewBag.TotalProducts = db.Foods.Count();
            ViewBag.TotalIngredients = db.Ingredients.Count();

            // 2. Thống kê đơn hàng
            // Lấy StatusId từ OrderStatus
            var placedStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Đặt hàng thành công");
            var preparingStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Đang chuẩn bị đơn hàng");
            var shippingStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Đang giao hàng");
            var completedStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Giao hàng thành công");

            ViewBag.PlacedOrders = placedStatus != null ? db.Orders.Count(o => o.StatusId == placedStatus.StatusId) : 0;
            ViewBag.PreparingOrders = preparingStatus != null ? db.Orders.Count(o => o.StatusId == preparingStatus.StatusId) : 0;
            ViewBag.ShippingOrders = shippingStatus != null ? db.Orders.Count(o => o.StatusId == shippingStatus.StatusId) : 0;
            ViewBag.CompletedOrders = completedStatus != null ? db.Orders.Count(o => o.StatusId == completedStatus.StatusId) : 0;
            ViewBag.TotalOrders = db.Orders.Count();
            ViewBag.TotalSales = db.Orders.Any() ? db.Orders.Sum(o => o.TotalAmount) : 0m;

            // 3. Doanh thu theo tháng (sử dụng lớp Monthly)
            var monthlySales = db.Orders
                .Where(o => o.OrderDate.Year == DateTime.Now.Year)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new Monthly
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(g => g.Month)
                .ToList();

            ViewBag.MonthlySales = Enumerable.Range(1, 12)
                .Select(m =>
                {
                    var sale = monthlySales.FirstOrDefault(x => x.Month == m);
                    return new Monthly
                    {
                        Month = m,
                        TotalRevenue = sale != null ? sale.TotalRevenue : 0m
                    };
                })
                .ToList();

            ViewBag.MonthlyBudget = Enumerable.Range(1, 12)
                .Select(m => new MonthlyBudget
                {
                    Month = m,
                    Budget = 3000000m + (m * 500000m)
                })
                .ToList();

            // 4. Sản phẩm bán chạy
            var invoiceData = db.InvoiceDetails
                            .Where(x => x.FoodId != null)
                            .Select(x => new
                            {
                                FoodId = x.FoodId.Value,
                                Quantity = x.SoLuong
                            });

            var orderData = db.OrderDetails
                .Select(x => new
                {
                    FoodId = x.FoodId,
                    Quantity = x.Quantity
                });

            var bestSellers = invoiceData
                .Union(orderData)
                .GroupBy(x => x.FoodId)
                .Select(g => new
                {
                    FoodId = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(8)
                .Join(db.Foods,
                    bs => bs.FoodId,
                    food => food.FoodId,
                    (bs, food) => new BanChayModel
                    {
                        FoodId = food.FoodId,
                        FoodName = food.FoodName,
                        ImageUrl = food.ImageURL,
                        Price = food.Price,
                        TotalSold = bs.TotalSold
                    })
                .ToList();

            ViewBag.BanChay = bestSellers;

            // 5. Nguyên liệu sắp hết
            ViewBag.LowStockIngredients = db.Ingredients
                .Where(i => i.SoLuong < 10)
                .ToList();

            // 6. Top Countries (dựa trên Address của Users)
            var topCountries = db.Orders
                .Join(db.Users, o => o.UserId, u => u.Id, (o, u) => new { o, u })
                .GroupBy(x => x.u.Address ?? "Unknown")
                .Select(g => new TopCountryModel
                {
                    Country = g.Key,
                    OrderCount = g.Count()
                })
                .OrderByDescending(g => g.OrderCount)
                .Take(5)
                .ToList();

            ViewBag.TopCountries = topCountries;

            // 7. Đơn hàng gần đây
            var recentOrders = db.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList()
                .Select(o =>
                {
                    var user = db.Users.FirstOrDefault(u => u.Id == o.UserId);
                    var status = db.OrderStatus.FirstOrDefault(s => s.StatusId == o.StatusId);
                    return new RecentOrderModel
                    {
                        OrderId = o.OrderId,
                        FullName = user != null ? user.FullName : "Unknown",
                        Status = status != null ? status.StatusName : "Unknown",
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount
                    };
                })
                .ToList();

            ViewBag.RecentOrders = recentOrders;

            // 8. Giả lập danh sách khách hàng cần hỗ trợ
            ViewBag.CustomersNeedHelp = new List<dynamic>
            {
                new { CustomerName = "Laila Tazkiah", Message = "My order hasn't arrived yet", TimeAgo = "1 min ago" },
                new { CustomerName = "Rizal Fakhri", Message = "Please cancel my order", TimeAgo = "2 hours ago" },
                new { CustomerName = "Syahdan Ubaidillah", Message = "Do you see my mother?", TimeAgo = "6 hours ago" }
            };

            return View();
        }

        [HttpGet]
        public ActionResult GlobalSearch(string query, string filterType)
        {
            if (string.IsNullOrEmpty(query))
            {
                TempData["Error"] = "Vui lòng nhập từ khóa tìm kiếm.";
                return RedirectToAction("HomeAdmin");
            }

            switch (filterType)
            {
                case "food":
                    return RedirectToAction("Info", "SanPhamAdmin", new { area = "Admin", search = query });
                case "invoice":
                    return RedirectToAction("HoaDon", "BaoCao", new { area = "Admin", search = query });
                case "staff":
                    return RedirectToAction("NhanVien", "NhanVien", new { area = "Admin", search = query });
                case "ingredient":
                    return RedirectToAction("KhoAdmin", "KhoAdmin", new { area = "Admin", search = query });
                default:
                    return RedirectToAction("HomeAdmin", "HomeAdmin", new { area = "Admin", search = query });
            }
        }
    }
}
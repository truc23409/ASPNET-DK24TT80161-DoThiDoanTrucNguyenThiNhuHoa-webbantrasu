using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    public class BaoCaoController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        // 🏆 Hiển thị trang tổng hợp báo cáo
        public ActionResult BaoCao()
        {
            return View();
        }

      
        public ActionResult DoanhThu(int? year)
        {
            // Nếu không có năm được chọn, mặc định là năm hiện tại
            int selectedYear = year ?? DateTime.Now.Year;

            // 1. Doanh thu theo tháng trong năm được chọn
            var monthlyRevenue = db.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order.OrderDate.Year == selectedYear)
                .GroupBy(od => od.Order.OrderDate.Month)
                .Select(g => new MonthlyRevenue
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(od => od.Price * od.Quantity)
                })
                .OrderBy(g => g.Month)
                .ToList();

            // Tạo danh sách doanh thu cho 12 tháng (nếu không có dữ liệu thì là 0)
            var monthlyRevenueList = Enumerable.Range(1, 12)
                .Select(m => new MonthlyRevenue
                {
                    Month = m,
                    TotalRevenue = monthlyRevenue.FirstOrDefault(x => x.Month == m)?.TotalRevenue ?? 0
                })
                .ToList();

            // 2. Số lượng sản phẩm bán được theo tháng trong năm được chọn
            var productSales = db.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Food)
                .Where(od => od.Order.OrderDate.Year == selectedYear)
                .GroupBy(od => new { od.FoodId, od.Food.FoodName, od.Food.ImageURL })
                .Select(g => new ProductSales
                {
                    FoodName = g.Key.FoodName,
                    ImageURL = g.Key.ImageURL,
                    SalesByMonth = g.GroupBy(x => x.Order.OrderDate.Month)
                                    .Select(m => new MonthlySalesReport
                                    {
                                        Month = m.Key,
                                        Quantity = m.Sum(x => x.Quantity)
                                    })
                                    .ToList(),
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(3) // Lấy 3 sản phẩm bán chạy nhất
                .ToList();

            // 3. Doanh thu 5 năm gần nhất
            int currentYear = DateTime.Now.Year;
            var yearlyRevenue = db.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order.OrderDate.Year >= currentYear - 4 && od.Order.OrderDate.Year <= currentYear)
                .GroupBy(od => od.Order.OrderDate.Year)
                .Select(g => new YearlyRevenue
                {
                    Year = g.Key,
                    TotalRevenue = g.Sum(od => od.Price * od.Quantity)
                })
                .OrderBy(g => g.Year)
                .ToList();

            // Tạo danh sách doanh thu cho 5 năm (nếu không có dữ liệu thì là 0)
            var yearlyRevenueList = Enumerable.Range(currentYear - 4, 5)
                .Select(y => new YearlyRevenue
                {
                    Year = y,
                    TotalRevenue = yearlyRevenue.FirstOrDefault(x => x.Year == y)?.TotalRevenue ?? 0
                })
                .ToList();

            // Truyền dữ liệu vào ViewBag
            ViewBag.SelectedYear = selectedYear;
            ViewBag.MonthlyRevenue = monthlyRevenueList;
            ViewBag.ProductSales = productSales;
            ViewBag.YearlyRevenue = yearlyRevenueList;

            return View();
        }

        public ActionResult BanChay()
        {
            var bestSellers = db.OrderDetails
                .GroupBy(d => d.FoodId)
                .Select(g => new
                {
                    FoodId = g.Key,
                    TotalSold = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(8)
                .ToList();

            var danhSachBanChay = bestSellers
                .Join(db.Foods,
                      d => d.FoodId,
                      f => f.FoodId,
                      (d, f) => new WebBanHang.Models.BanChayModel
                      {
                          FoodId = f.FoodId,
                          FoodName = f.FoodName,
                          ImageUrl = f.ImageURL,
                          Price = f.Price,
                          TotalSold = d.TotalSold
                      })
                .ToList();

            if (danhSachBanChay.Any())
            {
                return View(danhSachBanChay);
            }
            else
            {
                ViewBag.ErrorMessage = "Không có sản phẩm bán chạy.";
                return View(new List<WebBanHang.Models.BanChayModel>());
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class SanPhamController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        // Hiển thị danh sách sản phẩm với tùy chọn tìm kiếm và lọc
        public ActionResult SanPham(string searchString, int? categoryId, int? page)
        {
            var list = db.Foods.AsQueryable();

            // Lọc theo tên món ăn
            if (!string.IsNullOrEmpty(searchString))
            {
                list = list.Where(f => f.FoodName.Contains(searchString));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                list = list.Where(f => f.CategoryId == categoryId);
            }

            int pageSize = 12;
            int pageNumber = (page ?? 1);

            // Sắp xếp trước khi phân trang để LINQ to Entities hỗ trợ Skip và Take
            list = list.OrderBy(f => f.FoodId); // Sắp xếp theo FoodId (hoặc một trường khác nếu cần)

            var paginatedList = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            // Lấy tổng số trang
            ViewBag.TotalPages = (int)Math.Ceiling((double)list.Count() / pageSize);
            ViewBag.CurrentPage = pageNumber;

            // Lấy danh sách danh mục để hiển thị trên giao diện
            ViewBag.Categories = db.Categories.ToList();

            return View(paginatedList);
        }

        // Hành động tìm kiếm nhanh
        public ActionResult Search(string query)
        {
            var list = db.Foods.Where(f => f.FoodName.Contains(query)).ToList();
            return View("SanPham", list);
        }

        public ActionResult CaPhe()
        {
            var products = db.Foods.Where(x => x.Category.CategoryName == "Cà Phê").ToList();
            return View(products);
        }

        public ActionResult TraSua()
        {
            var products = db.Foods.Where(x => x.Category.CategoryName == "Trà Sữa").ToList();
            return View(products);
        }

        public ActionResult ThucUongDaXay()
        {
            var products = db.Foods.Where(x => x.Category.CategoryName == "Thức uống đá xay").ToList();
            return View(products);
        }

        public ActionResult BanhSnack()
        {
            var products = db.Foods.Where(x => x.Category.CategoryName == "Bánh & Snack").ToList();
            return View(products);
        }

        public ActionResult TraTraiCay()
        {
            var products = db.Foods.Where(x => x.Category.CategoryName == "Trà trái cây").ToList();
            return View(products);
        }

        public ActionResult ChiTiet(int id)
        {
            using (var db = new WebAppDBEntities())
            {
                var food = db.Foods.FirstOrDefault(f => f.FoodId == id);

                if (food == null)
                {
                    return HttpNotFound();
                }
                var relatedProducts = db.Foods
                    .Where(f => f.CategoryId == food.CategoryId && f.FoodId != id)
                    .Take(6)
                    .ToList();

                // Gán dữ liệu vào ViewBag
                ViewBag.SanPhamlienquan = relatedProducts;

                // Lấy toàn bộ Size và Topping
                var sizes = db.Sizes.ToList();
                var toppings = db.Toppings.ToList();

                ViewBag.Sizes = sizes;
                ViewBag.Toppings = toppings;

                return View(food);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
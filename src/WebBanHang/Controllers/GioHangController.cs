using System;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.Entity;

namespace WebBanHang.Controllers
{
    public class GioHangController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        [HttpPost]
        public ActionResult ThemVaoGio(int foodId, int soLuong, int? sizeId, List<int> toppingIds)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập để thêm vào giỏ hàng!" }, JsonRequestBehavior.AllowGet);
                }

                if (!int.TryParse(Session["Id"].ToString(), out int userId))
                {
                    return Json(new { success = false, message = "Lỗi khi lấy thông tin tài khoản!" });
                }

                var food = db.Foods.FirstOrDefault(f => f.FoodId == foodId);
                if (food == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không hợp lệ!" });
                }

                if (soLuong <= 0)
                {
                    return Json(new { success = false, message = "Số lượng không hợp lệ!" });
                }

                decimal sizePrice = 0;
                decimal toppingTotalPrice = 0;

                if (sizeId.HasValue && sizeId != 0)
                {
                    var size = db.Sizes.FirstOrDefault(s => s.SizeID == sizeId);
                    if (size != null)
                    {
                        sizePrice = size.ExtraPrice;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Kích thước không hợp lệ!" });
                    }
                }
                else
                {
                    sizeId = null;
                }

                if (toppingIds != null && toppingIds.Any())
                {
                    toppingTotalPrice = db.Toppings
                                        .Where(t => toppingIds.Contains(t.ToppingID))
                                        .Sum(t => t.ToppingPrice);
                }

                decimal itemTotalPrice = (food.Price + sizePrice + toppingTotalPrice) * soLuong;

                toppingIds = toppingIds?.OrderBy(t => t).ToList() ?? new List<int>();

                var gioHangItems = db.GioHangs
                    .Include(g => g.GioHang_Topping)
                    .Where(g => g.Id == userId && g.FoodId == foodId && g.SizeID == sizeId)
                    .ToList();

                GioHang gioHangItem = null;

                foreach (var item in gioHangItems)
                {
                    var existingToppingIds = item.GioHang_Topping
                        .Select(gt => gt.ToppingID)
                        .OrderBy(t => t)
                        .ToList();

                    if (existingToppingIds.SequenceEqual(toppingIds))
                    {
                        gioHangItem = item;
                        break;
                    }
                }

                if (gioHangItem != null)
                {
                    gioHangItem.SoLuong += soLuong;
                    gioHangItem.TotalPrice += itemTotalPrice;
                    db.SaveChanges();
                }
                else
                {
                    gioHangItem = new GioHang
                    {
                        Id = userId,
                        FoodId = foodId,
                        SoLuong = soLuong,
                        SizeID = sizeId,
                        TotalPrice = itemTotalPrice
                    };
                    db.GioHangs.Add(gioHangItem);
                    db.SaveChanges();

                    if (toppingIds != null && toppingIds.Any())
                    {
                        foreach (var toppingId in toppingIds)
                        {
                            db.GioHang_Topping.Add(new GioHang_Topping
                            {
                                GioHangID = gioHangItem.GioHangID,
                                ToppingID = toppingId
                            });
                        }
                        db.SaveChanges();
                    }
                }

                int tongSoLuong = db.GioHangs.Where(g => g.Id == userId).Sum(g => (int?)g.SoLuong).GetValueOrDefault();
                Session["SoLuongGioHang"] = tongSoLuong;

                return Json(new { success = true, cartCount = tongSoLuong });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        public ActionResult GioHang()
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Login", "Login", new { returnUrl = Request.Url.PathAndQuery });
            }

            int userId = Convert.ToInt32(Session["Id"]);

            var gioHangItems = db.GioHangs
                .Where(gh => gh.Id == userId)
                .Include(gh => gh.Food)
                .Include(gh => gh.Size)
                .Include(gh => gh.GioHang_Topping.Select(gt => gt.Topping))
                .ToList();

            int tongSoLuong = gioHangItems.Sum(g => g.SoLuong);
            Session["SoLuongGioHang"] = tongSoLuong;

            var cartList = gioHangItems.Select(g => new GioHang
            {
                GioHangID = g.GioHangID,
                Id = g.Id,
                FoodId = g.FoodId,
                Food = new Food
                {
                    FoodId = g.Food.FoodId,
                    FoodName = g.Food.FoodName,
                    ImageURL = g.Food.ImageURL,
                    Price = g.Food.Price
                },
                SoLuong = g.SoLuong,
                SizeID = g.SizeID,
                Size = g.Size != null ? new Size { SizeName = g.Size.SizeName, ExtraPrice = g.Size.ExtraPrice } : null,
                GioHang_Topping = g.GioHang_Topping.Select(gt => new GioHang_Topping
                {
                    Topping = new Topping { ToppingID = gt.ToppingID,
                        ToppingName = gt.Topping.ToppingName,
                        ToppingPrice = gt.Topping.ToppingPrice }
                }).ToList(),
                TotalPrice = g.TotalPrice
            }).ToList();

            return View(cartList);
        }

        [HttpPost]
        public ActionResult CapNhatSoLuong(int foodId, int? sizeId, int soLuong, List<int> toppingIds)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập." });
                }

                int userId = Convert.ToInt32(Session["Id"]);

                // Xử lý sizeId
                if (sizeId == 0)
                {
                    sizeId = null;
                }

                // Xử lý toppingIds null và sort
                if (toppingIds == null)
                {
                    toppingIds = new List<int>();
                }
                toppingIds = toppingIds.OrderBy(t => t).ToList();

                // Tìm sản phẩm trong giỏ hàng
                var gioHangItems = db.GioHangs
                    .Include(g => g.Food)
                    .Include(g => g.Size)
                    .Include(g => g.GioHang_Topping.Select(gt => gt.Topping))
                    .Where(g => g.Id == userId && g.FoodId == foodId && g.SizeID == sizeId)
                    .ToList();

                GioHang gioHangItem = null;

                foreach (var item in gioHangItems)
                {
                    var existingToppingIds = item.GioHang_Topping
                        .Select(gt => gt.ToppingID)
                        .OrderBy(t => t)
                        .ToList();

                    if (existingToppingIds.SequenceEqual(toppingIds))
                    {
                        gioHangItem = item;
                        break;
                    }
                }

                if (gioHangItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng!" });
                }

                if (soLuong <= 0)
                {
                    return Json(new { success = false, message = "Số lượng không hợp lệ!" });
                }

                // Cập nhật số lượng
                gioHangItem.SoLuong = soLuong;

                // Tính lại tổng tiền
                decimal sizePrice = gioHangItem.Size?.ExtraPrice ?? 0;
                decimal toppingTotalPrice = gioHangItem.GioHang_Topping.Sum(gt => gt.Topping.ToppingPrice);
                decimal itemTotalPrice = (gioHangItem.Food.Price + sizePrice + toppingTotalPrice) * soLuong;
                gioHangItem.TotalPrice = itemTotalPrice;

                db.SaveChanges();

                // Tính tổng số lượng và tổng tiền trong giỏ hàng
                int tongSoLuong = db.GioHangs.Where(g => g.Id == userId).Sum(g => (int?)g.SoLuong) ?? 0;
                decimal newTotal = db.GioHangs.Where(g => g.Id == userId).Sum(g => (decimal?)g.TotalPrice) ?? 0;

                Session["SoLuongGioHang"] = tongSoLuong;

                return Json(new
                {
                    success = true,
                    cartCount = tongSoLuong,
                    newItemTotal = itemTotalPrice,
                    newTotal = newTotal
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult XoaKhoiGio(int foodId, int? sizeId, List<int> toppingIds)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập." });
                }

                int userId = Convert.ToInt32(Session["Id"]);

                if (sizeId == 0)
                {
                    sizeId = null;
                }

                toppingIds = toppingIds?.OrderBy(t => t).ToList() ?? new List<int>();

                var gioHangItems = db.GioHangs
                    .Include(g => g.GioHang_Topping)
                    .Where(g => g.Id == userId && g.FoodId == foodId && g.SizeID == sizeId)
                    .ToList();

                GioHang gioHangItem = null;

                foreach (var item in gioHangItems)
                {
                    var existingToppingIds = item.GioHang_Topping
                        .Select(gt => gt.ToppingID)
                        .OrderBy(t => t)
                        .ToList();

                    if (existingToppingIds.SequenceEqual(toppingIds))
                    {
                        gioHangItem = item;
                        break;
                    }
                }

                if (gioHangItem != null)
                {
                    db.GioHang_Topping.RemoveRange(gioHangItem.GioHang_Topping);
                    db.GioHangs.Remove(gioHangItem);
                    db.SaveChanges();

                    int tongSoLuong = db.GioHangs.Where(g => g.Id == userId).Sum(g => (int?)g.SoLuong).GetValueOrDefault();
                    decimal newTotal = db.GioHangs.Where(g => g.Id == userId).Sum(g => (decimal?)g.TotalPrice).GetValueOrDefault();
                    Session["SoLuongGioHang"] = tongSoLuong;

                    return Json(new { success = true, cartCount = tongSoLuong, newTotal = newTotal });
                }

                return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult GetCartCount()
        {
            if (Session["Id"] == null)
            {
                return Json(new { cartCount = 0 }, JsonRequestBehavior.AllowGet);
            }

            int userId = Convert.ToInt32(Session["Id"]);
            int tongSoLuong = db.GioHangs.Where(g => g.Id == userId).Sum(g => (int?)g.SoLuong).GetValueOrDefault();
            Session["SoLuongGioHang"] = tongSoLuong;

            return Json(new { cartCount = tongSoLuong }, JsonRequestBehavior.AllowGet);
        }
    }
}
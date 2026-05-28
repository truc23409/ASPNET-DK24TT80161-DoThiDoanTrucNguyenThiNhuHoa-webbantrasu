using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;
using System.Data.Entity;

namespace WebBanHang.Areas.Admin.Controllers
{
    public class DonHangOfflineController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult DanhSachBan()
        {
            var danhSachBan = db.TableFoods.ToList();
            return PartialView("_DanhSachBanPartial", danhSachBan);
        }

        [HttpPost]
        public JsonResult ThemBan()
        {
            try
            {
                var soBan = db.TableFoods.Count() + 1;
                var banMoi = new TableFood
                {
                    TableName = "Table " + soBan,
                    TrangThai = "Trống"
                };
                db.TableFoods.Add(banMoi);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public JsonResult XoaBan()
        {
            try
            {
                var banCuoi = db.TableFoods.OrderByDescending(b => b.TableId).FirstOrDefault();
                if (banCuoi != null)
                {
                    db.TableFoods.Remove(banCuoi);
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không còn bàn để xóa" });
                }
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi xóa bàn" });
            }
        }

        [HttpPost]
        public ActionResult TraBan(int tableId)
        {
            var table = db.TableFoods.Find(tableId);
            if (table != null)
            {
                table.TrangThai = "False";
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public ActionResult DanhSachLoaiSanPham()
        {
            var danhSachLoai = db.Categories.ToList();
            return PartialView("_DanhSachLoaiSanPhamPartial", danhSachLoai);
        }

        public ActionResult DanhSachSanPham(int? categoryId)
        {
            try
            {
                if (categoryId == null)
                {
                    throw new Exception("categoryId is null");
                }

                var danhSachMon = db.Foods
                    .Where(f => f.CategoryId == categoryId)
                    .ToList();

                return PartialView("_DanhSachSanPhamPartial", danhSachMon);
            }
            catch (Exception ex)
            {
                return Content("Lỗi: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        [HttpGet]
        public JsonResult GetFoodByCategory(int categoryId)
        {
            var foods = db.Foods.Where(f => f.CategoryId == categoryId)
                                .Select(f => new { f.FoodId, f.FoodName, f.Price })
                                .ToList();
            return Json(foods, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetHoaDonByBan(int tableId)
        {
            var ban = db.TableFoods.Find(tableId);
            if (ban != null)
            {
                return Json(new { success = true, tableId = ban.TableId, tableName = ban.TableName, trangThai = ban.TrangThai }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "Không tìm thấy bàn" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ThanhToan(int tableId, List<int> foodIds)
        {
            try
            {
                if (tableId <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn bàn!" });
                }

                if (foodIds == null || !foodIds.Any())
                {
                    return Json(new { success = false, message = "Hóa đơn rỗng!" });
                }

                var ban = db.TableFoods.Find(tableId);
                if (ban != null)
                {
                    ban.TrangThai = "False";
                    db.SaveChanges();
                }

                return Json(new { success = true, message = "Thanh toán thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public ActionResult GetInvoicesByTableId(int tableId)
        {
            var invoice = db.Invoices
                .Include(i => i.InvoiceDetails.Select(d => d.Food))
                .FirstOrDefault(i => i.TableId == tableId && i.TrangThai == 0);

            if (invoice == null)
            {
                return PartialView("_InvoicePartial", null);
            }

            return PartialView("_InvoicePartial", invoice);
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
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    public class PhuongThucThanhToanController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        // Hiển thị danh sách phương thức thanh toán
        public ActionResult Index()
        {
            var paymentMethods = db.PhuongThucThanhToans.ToList();
            return View(paymentMethods);
        }

        // Thêm phương thức thanh toán - GET
        public ActionResult Add()
        {
            return View();
        }

        // Thêm phương thức thanh toán - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(PhuongThucThanhToan paymentMethod)
        {
            // Kiểm tra trùng tên phương thức thanh toán
            if (db.PhuongThucThanhToans.Any(p => p.TenPhuongThuc.ToLower() == paymentMethod.TenPhuongThuc.ToLower()))
            {
                TempData["ErrorMessage"] = "Tên phương thức thanh toán đã tồn tại. Vui lòng chọn tên khác.";
                return View(paymentMethod);
            }

            if (ModelState.IsValid)
            {
                db.PhuongThucThanhToans.Add(paymentMethod);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm phương thức thanh toán thành công!";
                return RedirectToAction("Index");
            }

            // Nếu có lỗi, hiển thị thông báo chi tiết
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm phương thức thanh toán: " + string.Join(" ", errors);
            return View(paymentMethod);
        }

        // Action để kiểm tra trùng tên qua AJAX
        [HttpGet]
        public JsonResult CheckTenPhuongThuc(string tenPhuongThuc)
        {
            bool isExist = db.PhuongThucThanhToans.Any(p => p.TenPhuongThuc.ToLower() == tenPhuongThuc.ToLower());
            return Json(!isExist, JsonRequestBehavior.AllowGet);
        }

        // Các action khác (Edit, Delete, v.v.) giữ nguyên
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var paymentMethod = db.PhuongThucThanhToans.Find(id);
            if (paymentMethod == null) return HttpNotFound();

            return View(paymentMethod);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PhuongThucThanhToan paymentMethod)
        {
            // Kiểm tra trùng tên (trừ chính phương thức đang chỉnh sửa)
            if (db.PhuongThucThanhToans.Any(p => p.TenPhuongThuc.ToLower() == paymentMethod.TenPhuongThuc.ToLower() && p.Id != paymentMethod.Id))
            {
                TempData["ErrorMessage"] = "Tên phương thức thanh toán đã tồn tại. Vui lòng chọn tên khác.";
                return View(paymentMethod);
            }

            if (ModelState.IsValid)
            {
                db.Entry(paymentMethod).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật phương thức thanh toán thành công!";
                return RedirectToAction("Index");
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật phương thức thanh toán: " + string.Join(" ", errors);
            return View(paymentMethod);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var paymentMethod = db.PhuongThucThanhToans.Find(id);
            if (paymentMethod == null) return HttpNotFound();

            return View(paymentMethod);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var paymentMethod = db.PhuongThucThanhToans.Find(id);
            if (paymentMethod == null)
            {
                TempData["ErrorMessage"] = "Phương thức thanh toán không tồn tại.";
                return RedirectToAction("Index");
            }

            db.PhuongThucThanhToans.Remove(paymentMethod);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Xóa phương thức thanh toán thành công!";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && db != null)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
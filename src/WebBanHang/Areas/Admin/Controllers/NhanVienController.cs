using System;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    public class NhanVienController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        // Hiển thị danh sách Staff
        public ActionResult NhanVien()
        {
            var staffs = db.Staffs.ToList();
            return View(staffs);
        }

        // GET: Hiển thị form thêm Staff
        [HttpGet]
        public ActionResult Add()
        {
            return View();
        }

        // POST: Xử lý thêm Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(Staff staff)
        {
            if (ModelState.IsValid)
            {
                db.Staffs.Add(staff);
                db.SaveChanges();
                return RedirectToAction("NhanVien");
            }
            return View(staff);
        }

        // GET: Hiển thị form sửa Staff
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var staff = db.Staffs.Find(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            return View(staff);
        }

        // POST: Xử lý sửa Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Staff staff)
        {
            if (ModelState.IsValid)
            {
                db.Entry(staff).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("NhanVien");
            }
            return View(staff);
        }

        // GET: Xác nhận xóa Staff
        [HttpGet]
        public ActionResult Delete(int id)
        {
            var staff = db.Staffs.Find(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            return View(staff);
        }

        // POST: Xử lý xóa Staff
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var staff = db.Staffs.Find(id);
            if (staff != null)
            {
                db.Staffs.Remove(staff);
                db.SaveChanges();
            }
            return RedirectToAction("NhanVien");
        }
    }
}

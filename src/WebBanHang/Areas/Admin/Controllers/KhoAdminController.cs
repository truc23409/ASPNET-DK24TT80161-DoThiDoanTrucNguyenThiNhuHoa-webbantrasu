using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    public class KhoAdminController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        public ActionResult KhoAdmin()
        {
            TempData["ErrorMessage"] = null;
            var list = db.Ingredients.ToList();
            return View(list);
        }

        // Thêm nguyên liệu (GET)
        public ActionResult Add()
        {
            return View(new Ingredient());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Add(Ingredient ingredient, List<int> selectedFoodIds, HttpPostedFileBase ImageFile)
        {
            try
            {
                if (!ValidateIngredient(ingredient))
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                }

                if (db.Ingredients.Any(i => i.IngredientName == ingredient.IngredientName))
                {
                    return Json(new { success = false, message = "Nguyên liệu này đã tồn tại." });
                }

                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string fileExt = Path.GetExtension(ImageFile.FileName).ToLower();
                    string[] allowedExts = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

                    if (!allowedExts.Contains(fileExt))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh hợp lệ." });
                    }

                    try
                    {
                        string newFileName = Guid.NewGuid().ToString() + fileExt;
                        string filePath = Path.Combine(Server.MapPath("~/Images/"), newFileName);
                        ImageFile.SaveAs(filePath);
                        ingredient.ImageURL = "/Images/" + newFileName;
                    }
                    catch (Exception imgEx)
                    {
                        return Json(new { success = false, message = "Lỗi khi lưu ảnh: " + imgEx.Message });
                    }
                }

                ingredient.LastUpdated = DateTime.Now;
                db.Ingredients.Add(ingredient);

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.SaveChanges();

                        if (selectedFoodIds != null && selectedFoodIds.Count > 0)
                        {
                            var foodIngredients = selectedFoodIds
                                .Where(foodId => foodId > 0)
                                .Select(foodId => new FoodIngredient { FoodId = foodId, IngredientId = ingredient.IngredientId })
                                .ToList();

                            db.FoodIngredients.AddRange(foodIngredients);
                        }

                        db.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Lỗi khi lưu dữ liệu: " + ex.Message });
                    }
                }

                return Json(new { success = true, message = "Thêm nguyên liệu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm nguyên liệu: " + ex.Message });
            }
        }


        // Chỉnh sửa nguyên liệu (GET)
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var ingredient = db.Ingredients.Find(id);
            if (ingredient == null) return HttpNotFound();

            // Lấy danh sách món ăn đang sử dụng nguyên liệu này
            ViewBag.SelectedFoodIds = db.FoodIngredients
                .Where(fi => fi.IngredientId == id)
                .Select(fi => fi.FoodId)
                .ToList();

            return View(ingredient);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(Ingredient ingredient, HttpPostedFileBase ImageFile)
        {
            try
            {
                var existingIngredient = db.Ingredients.Find(ingredient.IngredientId);
                if (existingIngredient == null)
                {
                    return Json(new { success = false, message = "Nguyên liệu không tồn tại." });
                }

                if (ingredient.SoLuong <= 0)
                {
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
                }

                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string fileExt = Path.GetExtension(ImageFile.FileName).ToLower();
                    string[] allowedExts = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

                    if (!allowedExts.Contains(fileExt))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (.png, .jpg, .jpeg, .gif, .webp)." });
                    }

                    if (!string.IsNullOrEmpty(existingIngredient.ImageURL))
                    {
                        string oldFilePath = Server.MapPath(existingIngredient.ImageURL);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    string newFileName = Guid.NewGuid().ToString() + fileExt;
                    string filePath = Path.Combine(Server.MapPath("~/Images/"), newFileName);
                    ImageFile.SaveAs(filePath);
                    existingIngredient.ImageURL = "/Images/" + newFileName;
                }

                existingIngredient.IngredientName = ingredient.IngredientName;
                existingIngredient.SoLuong = ingredient.SoLuong;
                existingIngredient.PhanLoai = ingredient.PhanLoai;
                existingIngredient.LastUpdated = DateTime.Now;

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CheckIngredientName(string name, int id)
        {
            bool exists = db.Ingredients.Any(x => x.IngredientName == name && x.IngredientId != id);
            return Json(new { exists = exists });
        }

        // Delete (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduct(int id)
        {
            var sp = db.Ingredients.Find(id);
            if (sp == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            // Xóa file ảnh
            if (!string.IsNullOrEmpty(sp.ImageURL))
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), sp.ImageURL.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            string tenSanPham = sp.IngredientName;
            db.Ingredients.Remove(sp);
            db.SaveChanges();

            return Json(new { success = true, message = $"Đã xóa sản phẩm {tenSanPham} thành công" });
        }

        private bool ValidateIngredient(Ingredient ingredient)
        {
            bool isValid = true;

            // Chuẩn hóa tên nguyên liệu để tránh lỗi trùng tên không mong muốn
            string normalizedIngredientName = ingredient.IngredientName?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(normalizedIngredientName))
            {
                ModelState.AddModelError("IngredientName", "Tên nguyên liệu không được để trống!");
                isValid = false;
            }
            else if (db.Ingredients.Any(i => i.IngredientName.Trim().ToLower() == normalizedIngredientName && i.IngredientId != ingredient.IngredientId))
            {
                ModelState.AddModelError("IngredientName", "Tên nguyên liệu đã tồn tại.");
                isValid = false;
            }

            if (ingredient.SoLuong < 1)
            {
                ModelState.AddModelError("SoLuong", "Số lượng phải lớn hơn 0!");
                isValid = false;
            }

            // Kiểm tra phân loại hợp lệ: chỉ được chứa chữ cái và khoảng trắng
            if (!string.IsNullOrEmpty(ingredient.PhanLoai) && !Regex.IsMatch(ingredient.PhanLoai.Trim(), @"^[a-zA-ZÀ-ỹ\s]+$"))
            {
                ModelState.AddModelError("PhanLoai", "Phân loại chỉ được chứa chữ cái và khoảng trắng, không được chứa số hoặc ký tự đặc biệt!");
                isValid = false;
            }

            return isValid;
        }

    }
}

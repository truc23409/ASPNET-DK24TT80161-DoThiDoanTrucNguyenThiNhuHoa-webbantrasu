using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebBanHang.Models;
using System.IO;

namespace WebBanHang.Controllers
{
    public class SanPhamAdminController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        // Danh sách món ăn
        public ActionResult Info()
        {
            var foodList = db.Foods.ToList();
            Debug.WriteLine("Số lượng món ăn trong DB: " + foodList.Count);

            if (!foodList.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào.";
            }

            return View(foodList);
        }

        // Thêm món ăn (GET)
        public ActionResult Add()
        {
            ViewBag.Categories = db.Categories.ToList();
            ViewBag.Ingredients = db.Ingredients.ToList();
            return View(new FoodAddViewModel { Food = new Food() });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Add(FoodAddViewModel model, HttpPostedFileBase ImageFile)
        {
            try
            {
                if (!ValidateFood(model.Food))
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                // Xử lý upload ảnh
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string fileExt = Path.GetExtension(ImageFile.FileName).ToLower();
                    string[] allowedExts = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

                    if (!allowedExts.Contains(fileExt))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (.png, .jpg, .jpeg, .gif, .webp)." });
                    }

                    string newFileName = Guid.NewGuid().ToString() + fileExt;
                    string filePath = Path.Combine(Server.MapPath("~/Images/"), newFileName);
                    ImageFile.SaveAs(filePath);
                    model.Food.ImageURL = "/Images/" + newFileName;
                }

                model.Food.UpdatedDate = DateTime.Now;
                db.Foods.Add(model.Food);
                db.SaveChanges();

                if (model.SelectedIngredientIds?.Any() == true)
                {
                    db.FoodIngredients.AddRange(model.SelectedIngredientIds.Select(ingredientId => new FoodIngredient
                    {
                        FoodId = model.Food.FoodId,
                        IngredientId = ingredientId,
                        Quantity = 1
                    }));
                    db.SaveChanges();
                }

                return Json(new { success = true, message = "Thêm món ăn thành công!", redirectUrl = Url.Action("Info") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm sản phẩm: " + ex.Message });
            }
        }



        // Chỉnh sửa món ăn (GET)
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var food = db.Foods.Find(id);
            if (food == null) return HttpNotFound();

            var model = new FoodEditViewModel
            {
                Food = food,
                SelectedIngredientIds = food.FoodIngredients.Select(fi => fi.IngredientId.Value).ToList()
            };

            ViewBag.Categories = db.Categories.ToList();
            ViewBag.Ingredients = db.Ingredients.ToList();
            return View(model);
        }

        // Chỉnh sửa món ăn (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(FoodEditViewModel model, HttpPostedFileBase ImageFile)
        {
            try
            {
                var food = db.Foods.Find(model.Food.FoodId);
                if (food == null)
                {
                    return Json(new { success = false, message = "Món ăn không tồn tại." });
                }

                if (model.Food.Stock <= 0)
                {
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
                }

                // Cập nhật thông tin món ăn
                food.FoodName = model.Food.FoodName;
                food.CategoryId = model.Food.CategoryId;
                food.Price = model.Food.Price;
                food.Discount = model.Food.Discount;
                food.Stock = model.Food.Stock;
                food.Description = model.Food.Description;
                food.Status = model.Food.Status;
                food.UpdatedDate = DateTime.Now;

                // Xử lý ảnh mới
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string fileExt = Path.GetExtension(ImageFile.FileName).ToLower();
                    string[] allowedExts = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

                    if (!allowedExts.Contains(fileExt))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (.png, .jpg, .jpeg, .gif, .webp)." });
                    }

                    if (!string.IsNullOrEmpty(food.ImageURL))
                    {
                        string oldFilePath = Server.MapPath(food.ImageURL);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    string newFileName = Guid.NewGuid().ToString() + fileExt;
                    string filePath = Path.Combine(Server.MapPath("~/Images/"), newFileName);
                    ImageFile.SaveAs(filePath);
                    food.ImageURL = "/Images/" + newFileName;
                }

                // Xóa nguyên liệu cũ
                db.FoodIngredients.RemoveRange(db.FoodIngredients.Where(fi => fi.FoodId == food.FoodId));

                // Cập nhật nguyên liệu mới
                if (model.SelectedIngredientIds?.Any() == true)
                {
                    db.FoodIngredients.AddRange(model.SelectedIngredientIds.Select(ingId => new FoodIngredient
                    {
                        FoodId = food.FoodId,
                        IngredientId = ingId,
                        Quantity = 1
                    }));
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật món ăn thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduct(int id)
        {
            using (var transaction = db.Database.BeginTransaction()) // Bắt đầu transaction để đảm bảo tính toàn vẹn dữ liệu
            {
                try
                {
                    var sp = db.Foods.FirstOrDefault(f => f.FoodId == id);
                    if (sp == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy món ăn." });
                    }

                    // Xóa dữ liệu liên quan trước khi xóa món ăn
                    var relatedFoodIngredients = db.FoodIngredients.Where(fi => fi.FoodId == id).ToList();
                    if (relatedFoodIngredients.Any())
                    {
                        db.FoodIngredients.RemoveRange(relatedFoodIngredients);
                        db.SaveChanges(); // Lưu trước để tránh lỗi ràng buộc
                    }

                    // Kiểm tra nếu món ăn có nguyên liệu liên quan
                    bool isIngredientUsedElsewhere = db.Foods.Any(f => f.IngredientId == sp.IngredientId && f.FoodId != id);
                    if (!isIngredientUsedElsewhere)
                    {
                        var relatedIngredients = db.Ingredients.Where(i => i.IngredientId == sp.IngredientId).ToList();
                        if (relatedIngredients.Any())
                        {
                            db.Ingredients.RemoveRange(relatedIngredients);
                        }
                    }

                    // Xóa ảnh nếu có
                    if (!string.IsNullOrEmpty(sp.ImageURL))
                    {
                        string fullPath = Path.Combine(Server.MapPath("~/"), sp.ImageURL.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }

                    // Xóa món ăn
                    db.Foods.Remove(sp);
                    db.SaveChanges();

                    transaction.Commit(); // Commit nếu không có lỗi

                    return Json(new { success = true, message = $"Đã xóa món ăn {sp.FoodName} thành công" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Hoàn tác nếu có lỗi
                    string errorMessage = ex.InnerException?.Message ?? ex.Message;
                    Console.WriteLine("Lỗi khi xóa món ăn: " + errorMessage);
                    return Json(new { success = false, message = "Lỗi khi xóa sản phẩm: " + errorMessage });
                }

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

        // ✅ Hàm kiểm tra hợp lệ dữ liệu món ăn
        private bool ValidateFood(Food food)
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(food.FoodName))
            {
                ModelState.AddModelError("FoodName", "Tên món ăn không được để trống!");
                isValid = false;
            }

            if (food.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá món ăn phải lớn hơn 0!");
                isValid = false;
            }

            if (!string.IsNullOrEmpty(food.ImageURL) && !Uri.TryCreate(food.ImageURL, UriKind.RelativeOrAbsolute, out _))
            {
                ModelState.AddModelError("ImageURL", "URL ảnh không hợp lệ!");
                isValid = false;
            }

            return isValid;
        }
    }
}

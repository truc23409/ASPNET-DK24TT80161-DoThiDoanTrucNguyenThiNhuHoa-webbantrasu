using System;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;
using System.Data.Entity;
using System.IO;
using System.Web;

namespace WebBanHang.Controllers
{
    public class TaiKhoanController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        public ActionResult TaiKhoan()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Login");
            }

            string username = Session["Username"].ToString();
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return RedirectToAction("Login", "Login");
            }

            return View(user);
        }

        [HttpPost]
        public JsonResult UpdateUser(int id, string field, string value)
        {
            if (Session["Username"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
            }

            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }

            string currentUsername = Session["Username"].ToString();
            string currentRole = Session["UserRole"]?.ToString() ?? "";

            if (currentRole != "Admin" && currentUsername != user.Username)
            {
                return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa!" });
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return Json(new { success = false, message = "Dữ liệu không được để trống!" });
            }

            try
            {
                switch (field)
                {
                    case "Username":
                        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z0-9]{3,20}$"))
                            return Json(new { success = false, message = "Username phải từ 3-20 ký tự, chỉ chứa chữ và số!" });
                        if (db.Users.Any(u => u.Username == value && u.Id != id))
                            return Json(new { success = false, message = "Username đã tồn tại!" });
                        string oldUsername = user.Username;
                        user.Username = value.Trim();
                        if (currentUsername == oldUsername)
                        {
                            Session["Username"] = user.Username;
                        }
                        break;

                    case "FullName":
                        if (value.Length < 3)
                            return Json(new { success = false, message = "Tên quá ngắn!" });
                        user.FullName = value.Trim();
                        break;

                    case "Email":
                        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                            return Json(new { success = false, message = "Email không hợp lệ!" });
                        if (db.Users.Any(u => u.Email == value && u.Id != id))
                            return Json(new { success = false, message = "Email đã tồn tại!" });
                        user.Email = value.Trim();
                        break;

                    case "Phone":
                        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^(0[3|5|7|8|9])[0-9]{8}$"))
                            return Json(new { success = false, message = "Số điện thoại không hợp lệ!" });
                        user.Phone = value.Trim();
                        break;

                    case "Address":
                        user.Address = value.Trim();
                        break;

                    default:
                        return Json(new { success = false, message = "Trường không hợp lệ!" });
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật {field}: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại!" });
            }
        }

        [HttpPost]
        public JsonResult UpdateAvatar(int id, HttpPostedFileBase AvatarFile)
        {
            if (Session["Username"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
            }

            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }

            string currentUsername = Session["Username"].ToString();
            string currentRole = Session["UserRole"]?.ToString() ?? "";

            if (currentRole != "Admin" && currentUsername != user.Username)
            {
                return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa!" });
            }

            try
            {
                if (AvatarFile != null && AvatarFile.ContentLength > 0)
                {
                    string fileExt = Path.GetExtension(AvatarFile.FileName).ToLower();
                    string[] allowedExts = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

                    if (!allowedExts.Contains(fileExt))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (.png, .jpg, .jpeg, .gif, .webp)." });
                    }

                    if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        string oldFilePath = Server.MapPath(user.AvatarUrl);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    string newFileName = Guid.NewGuid().ToString() + fileExt;
                    string filePath = Path.Combine(Server.MapPath("~/Images/"), newFileName);
                    AvatarFile.SaveAs(filePath);
                    user.AvatarUrl = "/Images/" + newFileName;

                    db.SaveChanges();
                    return Json(new { success = true, message = "Cập nhật ảnh đại diện thành công!", avatarUrl = user.AvatarUrl });
                }
                else
                {
                    return Json(new { success = false, message = "Vui lòng chọn một file ảnh!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật ảnh đại diện: " + ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetOrders(int page = 1, int pageSize = 5, string tab = "cho-xac-nhan")
        {
            if (Session["Username"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" }, JsonRequestBehavior.AllowGet);
            }

            string username = Session["Username"].ToString();
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng!" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var ordersQuery = db.Orders
                    .Include("PhuongThucThanhToan")
                    .Include("OrderDetails.Food")
                    .Include("OrderDetails.Size")
                    .Include("OrderDetails.Topping")
                    .Where(o => o.UserId == user.Id);

                // Lọc đơn hàng theo tab
                switch (tab)
                {
                    case "cho-xac-nhan":
                        ordersQuery = ordersQuery.Where(o => o.StatusId == 1); // Đặt hàng thành công
                        break;
                    case "dang-chuan-bi":
                        ordersQuery = ordersQuery.Where(o => o.StatusId == 2); // Đang chuẩn bị đơn hàng
                        break;
                    case "dang-giao-hang":
                        ordersQuery = ordersQuery.Where(o => o.StatusId == 3); // Đang giao hàng
                        break;
                    case "da-giao":
                        ordersQuery = ordersQuery.Where(o => o.StatusId == 4); // Giao hàng thành công
                        break;
                    case "da-huy":
                        ordersQuery = ordersQuery.Where(o => o.StatusId == 5); // Đã hủy
                        break;
                    default:
                        ordersQuery = ordersQuery.Where(o => o.StatusId == 1);
                        break;
                }

                // Tổng số đơn hàng (để tính số trang)
                int totalOrders = ordersQuery.Count();

                // Áp dụng phân trang
                ordersQuery = ordersQuery
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                var ordersList = ordersQuery.Select(o => new
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    StatusId = o.StatusId,
                    PaymentMethod = o.PhuongThucThanhToan != null ? o.PhuongThucThanhToan.TenPhuongThuc : "Không xác định",
                    OrderDetails = o.OrderDetails.Select(od => new
                    {
                        FoodName = od.Food != null ? od.Food.FoodName : "Không xác định",
                        SizeName = od.Size != null ? od.Size.SizeName : null,
                        ToppingName = od.Topping != null ? od.Topping.ToppingName : null,
                        Quantity = od.Quantity,
                        Price = od.Price
                    }).ToList()
                }).ToList();

                var orders = ordersList.Select(o =>
                {
                    var status = db.OrderStatus.FirstOrDefault(s => s.StatusId == o.StatusId);
                    return new
                    {
                        OrderId = o.OrderId,
                        OrderDate = o.OrderDate.ToString("o"),
                        TotalAmount = o.TotalAmount,
                        StatusId = o.StatusId,
                        Status = status != null ? status.StatusName : "Unknown",
                        PaymentMethod = o.PaymentMethod,
                        OrderDetails = o.OrderDetails
                    };
                }).ToList();

                return Json(new
                {
                    success = true,
                    orders = orders,
                    totalOrders = totalOrders,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalOrders / pageSize)
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lấy danh sách đơn hàng: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách đơn hàng: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult CancelOrder(int orderId)
        {
            if (Session["Username"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
            }

            string username = Session["Username"].ToString();
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }

            var order = db.Orders.FirstOrDefault(o => o.OrderId == orderId && o.UserId == user.Id);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc bạn không có quyền hủy!" });
            }

            // Chỉ cho phép hủy nếu đơn hàng đang ở trạng thái "Đặt hàng thành công" hoặc "Đang chuẩn bị đơn hàng"
            if (order.StatusId != 1 && order.StatusId != 2)
            {
                return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái hiện tại!" });
            }

            try
            {
                // Cập nhật trạng thái thành "Đã hủy" (StatusId = 5)
                order.StatusId = 5;
                db.SaveChanges();
                return Json(new { success = true, message = "Hủy đơn hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng: " + ex.Message });
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
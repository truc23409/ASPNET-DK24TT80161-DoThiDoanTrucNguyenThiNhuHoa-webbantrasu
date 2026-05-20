using System;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;
using BCrypt.Net; // Thêm namespace cho BCrypt

namespace WebBanHang.Controllers
{
    public class RegisterController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string Username, string Email, string Password, string FullName, string Phone, string Address)
        {
            // Kiểm tra Username đã tồn tại trong DB
            if (db.Users.Any(u => u.Username == Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại!");
            }

            // Kiểm tra Email đã tồn tại trong DB
            if (db.Users.Any(u => u.Email == Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng!");
            }

            // Nếu có lỗi thì trả về View
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Mã hóa mật khẩu bằng BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

            // Save vào DB
            User user = new User()
            {
                Username = Username,
                Email = Email,
                PasswordHash = hashedPassword, // Lưu mật khẩu đã băm
                FullName = FullName,
                Phone = Phone,
                Address = Address,
                Role = "User"
            };

            try
            {
                db.Users.Add(user);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                return View();
            }
        }
    }
}
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebBanHang.Models;
using BCrypt.Net;

namespace WebBanHang.Controllers
{
    public class LoginController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login", "Login");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, string returnUrl)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                bool isValid = false;
                try
                {
                    // Thử xác minh mật khẩu bằng BCrypt
                    isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Nếu PasswordHash không phải chuỗi băm hợp lệ, so sánh plaintext
                    isValid = (password == user.PasswordHash);
                    if (isValid)
                    {
                        // Nếu đăng nhập thành công bằng plaintext, băm lại mật khẩu và lưu vào DB
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                        db.SaveChanges();
                    }
                }

                if (isValid)
                {
                    // Debug: Kiểm tra vai trò
                    var role = user.Role?.Trim() ?? "NoRole";
                    System.Diagnostics.Debug.WriteLine($"Username: {user.Username}, Role: {role}");

                    // Lưu vai trò vào cookie
                    var userData = role;
                    var ticket = new FormsAuthenticationTicket(
                        1,
                        user.Username,
                        DateTime.Now,
                        DateTime.Now.AddMinutes(2880),
                        false,
                        userData,
                        FormsAuthentication.FormsCookiePath
                    );
                    var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                    if (encryptedTicket == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to encrypt ticket");
                        return Content("Lỗi: Không thể mã hóa ticket xác thực.");
                    }
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    Response.Cookies.Add(cookie);
                    System.Diagnostics.Debug.WriteLine("Cookie set successfully");

                    // Thiết lập Forms Authentication
                    FormsAuthentication.SetAuthCookie(user.Username, false);

                    // Lưu thông tin vào Session
                    Session["Id"] = user.Id.ToString();
                    Session["Username"] = user.Username;
                    Session["Role"] = user.Role;
                    Session["AvatarUrl"] = user.AvatarUrl; // Thêm AvatarUrl vào Session

                    // Chuyển hướng dựa trên vai trò
                    if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("HomeAdmin", "HomeAdmin", new { area = "Admin" });
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("Home", "Home");
                    }
                }
            }

            ViewBag.Message = "Sai thông tin đăng nhập!";
            return View();
        }
    }
}
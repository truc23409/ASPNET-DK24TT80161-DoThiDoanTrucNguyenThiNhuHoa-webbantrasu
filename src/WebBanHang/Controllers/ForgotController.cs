using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Configuration;
using System.Web.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class ForgotController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Forgot()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Forgot(string identifier)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == identifier || u.Phone == identifier);

            if (user != null)
            {
                // Sinh OTP và lưu vào DB
                string otp = new Random().Next(100000, 999999).ToString();
                user.OTPCode = otp;
                user.OTPExpiry = DateTime.Now.AddMinutes(5);
                db.SaveChanges();

                if (identifier.Contains("@"))
                {
                    // Gửi OTP qua Email
                    SendOTPEmail(user.Email, otp);
                }
                else
                {
                    // Gửi OTP qua SMS (demo)
                    SendSMS(user.Phone, $"Your OTP is: {otp}");
                }

                return RedirectToAction("VerifyOTP", new { phone = user.Phone });
            }
            else
            {
                ViewBag.Message = "Email or Phone not found.";
                return View();
            }
        }

        // 2. Verify OTP
        [HttpGet]
        public ActionResult VerifyOTP(string phone)
        {
            ViewBag.Phone = phone;
            return View();
        }

        [HttpPost]
        public ActionResult VerifyOTP(string phone, string otp)
        {
            var user = db.Users.FirstOrDefault(u => u.Phone == phone);
            if (user != null && user.OTPCode == otp && user.OTPExpiry > DateTime.Now)
            {
                // OTP hợp lệ
                return RedirectToAction("ResetPassword", new { phone = phone });
            }

            ViewBag.Message = "OTP invalid or expired!";
            ViewBag.Phone = phone;
            return View("VerifyOTP");
        }

        // 3. Reset Password
        [HttpGet]
        public ActionResult ResetPassword(string phone)
        {
            var user = db.Users.FirstOrDefault(u => u.Phone == phone);
            if (user == null) return HttpNotFound();

            ViewBag.Phone = phone;
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(string phone, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Message = "Passwords do not match.";
                ViewBag.Phone = phone;
                return View();
            }

            var user = db.Users.FirstOrDefault(u => u.Phone == phone);

            if (user != null)
            {
                user.PasswordHash = HashPassword(newPassword);
                user.OTPCode = null;
                db.SaveChanges();
                return RedirectToAction("Login", "Login");
            }

            ViewBag.Message = "Error occurred!";
            return View();
        }

        // Sửa thành lấy config từ Web.config
        public void SendOTPEmail(string toEmail, string otp)
        {
            var email = WebConfigurationManager.AppSettings["Email"];
            var password = WebConfigurationManager.AppSettings["AppPassword"];
            var host = WebConfigurationManager.AppSettings["Host"];
            var port = int.Parse(WebConfigurationManager.AppSettings["Port"]);

            var smtpClient = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(email, password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email),
                Subject = "Your OTP Code",
                Body = $"Your OTP code is: <b>{otp}</b>. It will expire in 5 minutes.",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);
            smtpClient.Send(mailMessage);
        }

        // Hàm gửi OTP SMS (demo)
        public void SendSMS(string phone, string message)
        {
            Console.WriteLine($"[SMS to {phone}]: {message}");
        }

        // Hàm hash password (ví dụ đơn giản)
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}

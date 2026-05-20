using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanHang.Models;
using System.Data.Entity;
using System.Text.RegularExpressions;
using System.Configuration;
using WebBanHang.Models.Payments;

namespace WebBanHang.Controllers
{
    public class ThanhToanController : Controller
    {
        private readonly WebAppDBEntities db = new WebAppDBEntities();

        [HttpPost]
        public JsonResult PrepareCheckout(List<int> selectedItems)
        {
            try
            {
                if (Session["Id"] == null || !int.TryParse(Session["Id"]?.ToString(), out int userId))
                {
                    return Json(new { success = false, message = "Bạn chưa đăng nhập!" }, JsonRequestBehavior.AllowGet);
                }

                if (selectedItems == null || !selectedItems.Any())
                {
                    return Json(new { success = false, message = "Không có sản phẩm nào được chọn!" }, JsonRequestBehavior.AllowGet);
                }

                var selectedCartItems = db.GioHangs
                                         .Include(g => g.Food)
                                         .Include(g => g.Size)
                                         .Where(g => g.Id == userId && selectedItems.Contains(g.GioHangID))
                                         .ToList();

                if (!selectedCartItems.Any())
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm được chọn trong giỏ hàng!" }, JsonRequestBehavior.AllowGet);
                }

                Session["SelectedCartItems"] = selectedItems;

                return Json(new { success = true, message = "Đã chuẩn bị dữ liệu thanh toán!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi chuẩn bị thanh toán!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ThanhToan()
        {
            if (Session["Id"] == null || !int.TryParse(Session["Id"]?.ToString(), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            var selectedItems = Session["SelectedCartItems"] as List<int>;
            if (selectedItems == null || !selectedItems.Any())
            {
                return RedirectToAction("GioHang", "GioHang");
            }

            var gioHang = db.GioHangs
                            .Include(g => g.Food)
                            .Include(g => g.Size)
                            .Where(g => g.Id == userId && selectedItems.Contains(g.GioHangID))
                            .ToList();

            if (gioHang.Count == 0)
            {
                return RedirectToAction("GioHang", "GioHang");
            }

            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Login");
            }

            var deliveryAddresses = db.DeliveryAddresses
                                     .Where(da => da.UserId == userId)
                                     .OrderByDescending(da => da.IsDefault)
                                     .ThenByDescending(da => da.CreatedDate)
                                     .ToList();
            ViewBag.DeliveryAddresses = deliveryAddresses;

            decimal totalAmount = gioHang.Sum(g => g.TotalPrice);

            var paymentMethods = db.PhuongThucThanhToans.ToList();
            ViewBag.PaymentMethods = paymentMethods;

            ViewBag.User = user;
            ViewBag.CartItems = gioHang;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.OrderDate = DateTime.Now;

            return View(user);
        }

        [HttpPost]
        public ActionResult ThanhToan(string address, string newAddress, int? paymentMethodId)
        {
            var code = new { Success = false, Code = -1 as int?, Url = "" };
            if (Session["Id"] == null || !int.TryParse(Session["Id"]?.ToString(), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            var selectedItems = Session["SelectedCartItems"] as List<int>;
            if (selectedItems == null || !selectedItems.Any())
            {
                return RedirectToAction("GioHang", "GioHang");
            }

            var gioHang = db.GioHangs
                            .Include(g => g.Food)
                            .Include(g => g.Size)
                            .Where(g => g.Id == userId && selectedItems.Contains(g.GioHangID))
                            .ToList();

            if (gioHang.Count == 0)
            {
                return RedirectToAction("GioHang", "GioHang");
            }

            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("ThanhToan");
            }

            // Kiểm tra paymentMethodId
            if (!paymentMethodId.HasValue)
            {
                TempData["Error"] = "Vui lòng chọn phương thức thanh toán!";
                return RedirectToAction("ThanhToan");
            }

            // Xử lý địa chỉ giao hàng
            string finalAddress;
            if (address == "new")
            {
                if (string.IsNullOrWhiteSpace(newAddress))
                {
                    TempData["Error"] = "Địa chỉ mới không được để trống!";
                    return RedirectToAction("ThanhToan");
                }

                var newDeliveryAddress = new DeliveryAddress
                {
                    UserId = userId,
                    Address = newAddress.Trim(),
                    IsDefault = false,
                    CreatedDate = DateTime.Now
                };
                db.DeliveryAddresses.Add(newDeliveryAddress);
                db.SaveChanges();

                finalAddress = newAddress.Trim();
            }
            else
            {
                var selectedAddress = db.DeliveryAddresses
                                       .FirstOrDefault(da => da.UserId == userId && da.AddressId.ToString() == address);
                if (selectedAddress == null)
                {
                    TempData["Error"] = "Địa chỉ không hợp lệ!";
                    return RedirectToAction("ThanhToan");
                }
                finalAddress = selectedAddress.Address;
            }

            user.Address = finalAddress;
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            // Tính tổng tiền bao gồm phí vận chuyển
            decimal subtotal = gioHang.Sum(g => g.TotalPrice);
            decimal shippingFee = 20000; // Phí vận chuyển cố định 20,000 VND
            decimal totalAmount = subtotal + shippingFee; // Tổng tiền bao gồm phí vận chuyển

            var status = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Đặt hàng thành công");
            if (status == null)
            {
                TempData["Error"] = "Không tìm thấy trạng thái 'Đặt hàng thành công' trong hệ thống!";
                return RedirectToAction("ThanhToan");
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount, // Lưu tổng tiền đã bao gồm phí vận chuyển
                PaymentMethodId = paymentMethodId.Value,
                StatusId = status.StatusId,
                DeliveryAddress = finalAddress
            };
            db.Orders.Add(order);
            db.SaveChanges();

            foreach (var item in gioHang)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    FoodId = item.FoodId,
                    SizeId = item.SizeID,
                    Quantity = item.SoLuong,
                    Price = item.TotalPrice / item.SoLuong // Giá đơn vị của sản phẩm
                };
                db.OrderDetails.Add(orderDetail);
            }
            db.SaveChanges();

            // Xử lý thanh toán VNPay
            if (paymentMethodId == 1) // ID của VNPay (dựa trên dữ liệu bạn insert: 'VN Pay' có ID = 1)
            {
                var url = UrlPayment(2, order.OrderId.ToString()); // Sử dụng OrderId thay vì Code
                if (string.IsNullOrEmpty(url))
                {
                    TempData["Error"] = "Lỗi khi tạo URL thanh toán VNPay!";
                    return RedirectToAction("ThanhToan");
                }
                code = new { Success = true, Code = paymentMethodId as int?, Url = url };
                return Json(code);
            }

            // Nếu không phải VNPay, tiếp tục xử lý bình thường
            // Xử lý thanh toán không phải VNPay
            db.GioHangs.RemoveRange(gioHang);
            db.SaveChanges();

            Session["SoLuong"] = 0;
            Session["SelectedCartItems"] = null;

            // Chuyển hướng trực tiếp đến ThanhCong
            return RedirectToAction("ThanhCong", "ThanhToan");
        }
        public ActionResult ThanhCong()
        {
            ViewBag.Message = "Bạn đã đặt hàng thành công lúc " + DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            return View();
        }

        [HttpPost]
        public JsonResult AddDeliveryAddress(string newAddress)
        {
            if (Session["Id"] == null || !int.TryParse(Session["Id"]?.ToString(), out int userId))
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" }, JsonRequestBehavior.AllowGet);
            }

            if (string.IsNullOrWhiteSpace(newAddress))
            {
                return Json(new { success = false, message = "Địa chỉ không được để trống!" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var newDeliveryAddress = new DeliveryAddress
                {
                    UserId = userId,
                    Address = newAddress.Trim(),
                    IsDefault = false,
                    CreatedDate = DateTime.Now
                };
                db.DeliveryAddresses.Add(newDeliveryAddress);
                db.SaveChanges();

                var deliveryAddresses = db.DeliveryAddresses
                                         .Where(da => da.UserId == userId)
                                         .OrderByDescending(da => da.IsDefault)
                                         .ThenByDescending(da => da.CreatedDate)
                                         .Select(da => new
                                         {
                                             AddressId = da.AddressId,
                                             Address = da.Address,
                                             IsDefault = da.IsDefault
                                         })
                                         .ToList();

                return Json(new { success = true, message = "Thêm địa chỉ thành công!", addresses = deliveryAddresses }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm địa chỉ!" }, JsonRequestBehavior.AllowGet);
            }
        }

        #region Thanh toán VNPay
        public ActionResult VnpayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }

                string orderIdStr = Convert.ToString(vnpay.GetResponseData("vnp_TxnRef"));
                if (!int.TryParse(orderIdStr, out int orderId))
                {
                    ViewBag.InnerText = "Mã đơn hàng không hợp lệ.";
                    return View();
                }

                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                string terminalID = Request.QueryString["vnp_TmnCode"];
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
                string bankCode = Request.QueryString["vnp_BankCode"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        var order = db.Orders.FirstOrDefault(x => x.OrderId == orderId);
                        if (order != null)
                        {
                            var status = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Đã thanh toán");
                            if (status != null)
                            {
                                order.StatusId = status.StatusId;
                                db.Entry(order).State = EntityState.Modified;
                                db.SaveChanges();

                                var selectedItems = Session["SelectedCartItems"] as List<int>;
                                if (selectedItems != null && selectedItems.Any())
                                {
                                    var gioHang = db.GioHangs
                                                    .Where(g => g.Id == order.UserId && selectedItems.Contains(g.GioHangID))
                                                    .ToList();
                                    db.GioHangs.RemoveRange(gioHang);
                                    db.SaveChanges();
                                }

                                Session["SoLuong"] = 0;
                                Session["SelectedCartItems"] = null;
                            }
                        }

                        ViewBag.InnerText = "Giao dịch được thực hiện thành công. Cảm ơn quý khách đã sử dụng dịch vụ";
                        ViewBag.OrderId = orderId;
                    }
                    else
                    {
                        ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xử lý. Mã lỗi: " + vnp_ResponseCode;
                    }

                    // Định dạng số tiền với dấu phân cách hàng nghìn
                    ViewBag.ThanhToanThanhCong = "Số tiền thanh toán (VND): " + vnp_Amount.ToString("N0");
                }
                else
                {
                    ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xác thực chữ ký.";
                }
            }

            return View();
        }

        public string UrlPayment(int type, string orderId)
        {
            // Kiểm tra orderId hợp lệ
            if (!int.TryParse(orderId, out int parsedOrderId))
            {
                return string.Empty;
            }

            var order = db.Orders.FirstOrDefault(x => x.OrderId == parsedOrderId);
            if (order == null)
            {
                return string.Empty;
            }

            // Lấy các thông tin cấu hình từ Web.config
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

            // Kiểm tra các giá trị cấu hình
            if (string.IsNullOrEmpty(vnp_Returnurl) || string.IsNullOrEmpty(vnp_Url) ||
                string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
            {
                return string.Empty; // Trả về rỗng nếu cấu hình bị thiếu
            }

            VnPayLibrary vnpay = new VnPayLibrary();

            // Thêm các tham số bắt buộc
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            // Kiểm tra và định dạng vnp_Amount
            decimal amount = order.TotalAmount; // Đảm bảo TotalAmount đã bao gồm phí vận chuyển
            long vnpAmount = (long)(amount * 100); // Nhân với 100 và ép kiểu về long để đảm bảo là số nguyên
            vnpay.AddRequestData("vnp_Amount", vnpAmount.ToString());

            // Định dạng ngày giờ
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");

            // Lấy địa chỉ IP
            string ipAddress = Utils.GetIpAddress();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "127.0.0.1"; // Giá trị mặc định nếu không lấy được IP
            }
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);

            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: " + order.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "250000");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString());

            // Tạo URL thanh toán
            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return paymentUrl;
        }
        #endregion

        [HttpPost]
        public JsonResult MuaNgay(int foodId, int soLuong, int? sizeId, List<int> toppingIds)
        {
            try
            {
                if (Session["Id"] == null || !int.TryParse(Session["Id"]?.ToString(), out int userId))
                {
                    return Json(new { success = false, message = "Bạn chưa đăng nhập!" }, JsonRequestBehavior.AllowGet);
                }

                var food = db.Foods.FirstOrDefault(f => f.FoodId == foodId);
                if (food == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm!" }, JsonRequestBehavior.AllowGet);
                }

                // Lấy CategoryId từ sản phẩm
                var categoryId = food.CategoryId;

                // Kiểm tra nếu CategoryId = 4, không yêu cầu size hoặc topping
                if (categoryId == 4 && sizeId.HasValue)
                {
                    return Json(new { success = false, message = "Sản phẩm này không yêu cầu kích thước!" }, JsonRequestBehavior.AllowGet);
                }

                var size = categoryId != 4 && sizeId.HasValue ? db.Sizes.FirstOrDefault(s => s.SizeID == sizeId) : null;
                if (categoryId != 4 && !sizeId.HasValue)
                {
                    return Json(new { success = false, message = "Vui lòng chọn kích thước sản phẩm!" }, JsonRequestBehavior.AllowGet);
                }
                if (categoryId != 4 && size == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy kích thước sản phẩm!" }, JsonRequestBehavior.AllowGet);
                }

                var existingCart = db.GioHangs.Where(g => g.Id == userId).ToList();
                db.GioHangs.RemoveRange(existingCart);
                db.SaveChanges();

                decimal totalPrice = food.Price;
                if (categoryId != 4 && size != null)
                {
                    totalPrice += size.ExtraPrice;
                }

                if (categoryId != 4 && toppingIds != null && toppingIds.Count > 0)
                {
                    var toppings = db.Toppings.Where(tp => toppingIds.Contains(tp.ToppingID)).ToList();
                    totalPrice += toppings.Sum(t => t.ToppingPrice);
                }

                totalPrice *= soLuong;

                var newCartItem = new GioHang
                {
                    Id = userId,
                    FoodId = foodId,
                    SizeID = categoryId != 4 && sizeId.HasValue ? sizeId : (int?)null, // Chỉ gán sizeId nếu cần
                    SoLuong = soLuong,
                    TotalPrice = totalPrice
                };

                db.GioHangs.Add(newCartItem);
                db.SaveChanges();

                Session["SelectedCartItems"] = new List<int> { newCartItem.GioHangID };
                Session["SoLuong"] = soLuong;

                if (categoryId != 4 && toppingIds != null && toppingIds.Count > 0)
                {
                    foreach (var toppingId in toppingIds)
                    {
                        var cartTopping = new GioHang_Topping
                        {
                            GioHangID = newCartItem.GioHangID,
                            ToppingID = toppingId
                        };
                        db.GioHang_Topping.Add(cartTopping);
                    }
                    db.SaveChanges();
                }

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!", redirectUrl = "/ThanhToan/ThanhToan" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log lỗi để debug (thêm log nếu cần)
                return Json(new { success = false, message = "Có lỗi xảy ra khi mua ngay!" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult UpdateUser(int id, string field, string value)
        {
            if (Session["Id"] == null || !int.TryParse(Session["Id"]?.ToString(), out int userId))
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" }, JsonRequestBehavior.AllowGet);
            }

            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng!" }, JsonRequestBehavior.AllowGet);
            }

            if (userId != id)
            {
                string currentRole = Session["Role"]?.ToString() ?? "";
                if (currentRole != "Admin")
                {
                    return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa thông tin này!" }, JsonRequestBehavior.AllowGet);
                }
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return Json(new { success = false, message = $"Trường {field} không được để trống!" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                switch (field)
                {
                    case "FullName":
                        if (value.Length < 3)
                            return Json(new { success = false, message = "Tên phải từ 3 ký tự trở lên!" }, JsonRequestBehavior.AllowGet);
                        user.FullName = value.Trim();
                        break;

                    case "Email":
                        if (!Regex.IsMatch(value, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                            return Json(new { success = false, message = "Email không hợp lệ!" }, JsonRequestBehavior.AllowGet);
                        if (db.Users.Any(u => u.Email == value && u.Id != id))
                            return Json(new { success = false, message = "Email đã tồn tại!" }, JsonRequestBehavior.AllowGet);
                        user.Email = value.Trim();
                        break;

                    case "Phone":
                        if (!Regex.IsMatch(value, @"^(0[3|5|7|8|9])[0-9]{8}$"))
                            return Json(new { success = false, message = "Số điện thoại không hợp lệ! Phải bắt đầu bằng 03, 05, 07, 08, 09 và có 10 chữ số." }, JsonRequestBehavior.AllowGet);
                        user.Phone = value.Trim();
                        break;

                    case "Address":
                        user.Address = value.Trim();
                        break;

                    default:
                        return Json(new { success = false, message = "Trường không hợp lệ!" }, JsonRequestBehavior.AllowGet);
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thành công!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật thông tin!" }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
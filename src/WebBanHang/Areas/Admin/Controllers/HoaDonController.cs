using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;
using ClosedXML.Excel;
using System.IO;

namespace WebBanHang.Areas.Admin.Controllers
{
    public class HoaDonController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        // Action hiển thị danh sách hóa đơn
        public ActionResult HoaDon(DateTime? startDate, DateTime? endDate)
        {
            var orders = db.Orders
                .Include(o => o.PhuongThucThanhToan)
                .Include(o => o.User)
                .Include(o => o.OrderStatu)
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                var endDateInclusive = endDate.Value.Date.AddDays(1).AddTicks(-1);
                orders = orders.Where(o => o.OrderDate >= startDate.Value && o.OrderDate <= endDateInclusive);

                ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            }

            var list = orders.OrderByDescending(o => o.OrderDate).ToList();

            return View(list);
        }

        // Action xuất danh sách hóa đơn ra Excel
        public ActionResult ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            var orders = db.Orders
                .Include(o => o.PhuongThucThanhToan)
                .Include(o => o.User)
                .Include(o => o.OrderStatu)
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                var endDateInclusive = endDate.Value.Date.AddDays(1).AddTicks(-1);
                orders = orders.Where(o => o.OrderDate >= startDate.Value && o.OrderDate <= endDateInclusive);
            }

            var orderList = orders.OrderByDescending(o => o.OrderDate).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Danh sách hóa đơn");
                var currentRow = 1;

                // Header
                worksheet.Cell(currentRow, 1).Value = "STT";
                worksheet.Cell(currentRow, 2).Value = "Mã hóa đơn";
                worksheet.Cell(currentRow, 3).Value = "Khách hàng";
                worksheet.Cell(currentRow, 4).Value = "Ngày đặt hàng";
                worksheet.Cell(currentRow, 5).Value = "Tổng tiền (VNĐ)";
                worksheet.Cell(currentRow, 6).Value = "Phương thức thanh toán";
                worksheet.Cell(currentRow, 7).Value = "Trạng thái";

                // Định dạng header
                for (int col = 1; col <= 7; col++)
                {
                    worksheet.Cell(currentRow, col).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, col).Style.Fill.BackgroundColor = XLColor.Black;
                    worksheet.Cell(currentRow, col).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(currentRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Dữ liệu
                int index = 1;
                foreach (var order in orderList)
                {
                    currentRow++;
                    var statusName = db.OrderStatus.FirstOrDefault(s => s.StatusId == order.StatusId)?.StatusName ?? "Unknown";

                    worksheet.Cell(currentRow, 1).Value = index++;
                    worksheet.Cell(currentRow, 2).Value = order.OrderId;
                    worksheet.Cell(currentRow, 3).Value = order.User?.FullName ?? "N/A";
                    worksheet.Cell(currentRow, 4).Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(currentRow, 5).Value = order.TotalAmount;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(currentRow, 6).Value = order.PhuongThucThanhToan?.TenPhuongThuc ?? "N/A";
                    worksheet.Cell(currentRow, 7).Value = statusName;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileName = $"DanhSachHoaDon_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(stream.ToArray(), contentType, fileName);
                }
            }
        }

        public ActionResult ExportChiTietToExcel(int orderId)
        {
            var chiTiet = db.OrderDetails
                .Include(od => od.Food)
                .Include(od => od.Size)
                .Include(od => od.Topping)
                .Where(c => c.OrderId == orderId)
                .ToList();

            if (!chiTiet.Any())
            {
                return Content("Không có dữ liệu chi tiết để xuất!");
            }

            var order = db.Orders.FirstOrDefault(o => o.OrderId == orderId);
            var user = order != null ? db.Users.FirstOrDefault(u => u.Id == order.UserId) : null;
            var status = order != null ? db.OrderStatus.FirstOrDefault(s => s.StatusId == order.StatusId) : null;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add($"ChiTietHoaDon_{orderId}");
                var currentRow = 1;

                // Tiêu đề - Merge các ô từ cột 1 đến cột 7
                worksheet.Range(currentRow, 1, currentRow, 7).Merge();
                worksheet.Cell(currentRow, 1).Value = $"CHI TIẾT HÓA ĐƠN #{orderId}";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentRow += 2;

                // Thông tin đơn hàng
                worksheet.Cell(currentRow, 1).Value = "Thông tin đơn hàng:";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Ngày đặt:";
                worksheet.Cell(currentRow, 2).Value = order?.OrderDate.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Khách hàng:";
                worksheet.Cell(currentRow, 2).Value = user?.FullName ?? "N/A";
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Số điện thoại:";
                worksheet.Cell(currentRow, 2).Value = user?.Phone ?? "N/A";
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Địa chỉ:";
                worksheet.Cell(currentRow, 2).Value = order?.DeliveryAddress ?? "N/A";
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Trạng thái:";
                worksheet.Cell(currentRow, 2).Value = status?.StatusName ?? "N/A";
                currentRow += 2;

                // Header bảng chi tiết
                worksheet.Cell(currentRow, 1).Value = "STT";
                worksheet.Cell(currentRow, 2).Value = "Tên sản phẩm";
                worksheet.Cell(currentRow, 3).Value = "Kích thước";
                worksheet.Cell(currentRow, 4).Value = "Topping";
                worksheet.Cell(currentRow, 5).Value = "Số lượng";
                worksheet.Cell(currentRow, 6).Value = "Giá (VNĐ)";
                worksheet.Cell(currentRow, 7).Value = "Thành tiền (VNĐ)";

                // Định dạng header
                for (int col = 1; col <= 7; col++)
                {
                    worksheet.Cell(currentRow, col).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, col).Style.Fill.BackgroundColor = XLColor.Black;
                    worksheet.Cell(currentRow, col).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(currentRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Dữ liệu chi tiết
                int index = 1;
                foreach (var item in chiTiet)
                {
                    currentRow++;

                    var toppingName = "Không có";
                    if (item.ToppingId.HasValue)
                    {
                        var topping = db.Toppings.FirstOrDefault(t => t.ToppingID == item.ToppingId);
                        toppingName = topping != null ? topping.ToppingName : "Không có";
                    }

                    worksheet.Cell(currentRow, 1).Value = index++;
                    worksheet.Cell(currentRow, 2).Value = item.Food != null ? item.Food.FoodName : "Không xác định";
                    worksheet.Cell(currentRow, 3).Value = item.Size != null ? item.Size.SizeName : "Không có";
                    worksheet.Cell(currentRow, 4).Value = toppingName;
                    worksheet.Cell(currentRow, 5).Value = item.Quantity;
                    worksheet.Cell(currentRow, 6).Value = item.Price;
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(currentRow, 7).Value = item.Quantity * item.Price;
                    worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
                }

                // Tổng cộng
                currentRow++;
                worksheet.Cell(currentRow, 6).Value = "Tổng cộng:";
                worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 7).Value = chiTiet.Sum(c => c.Quantity * c.Price);
                worksheet.Cell(currentRow, 7).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 7).Style.Font.FontColor = XLColor.Red;
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";

                // Đặt độ rộng cột tự động
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileName = $"ChiTietHoaDon_{orderId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(stream.ToArray(), contentType, fileName);
                }
            }
        }

        // Action lấy chi tiết hóa đơn qua AJAX (trả về PartialView)
        public ActionResult ChiTietHoaDon(int orderId)
        {
            try
            {
                var chiTiet = db.OrderDetails
                               .Include(od => od.Food)
                               .Include(od => od.Size)
                               .Include(od => od.Topping)
                               .Where(c => c.OrderId == orderId)
                               .ToList();

                if (!chiTiet.Any())
                {
                    return Content("<tr class='detail-row'><td colspan='7' class='text-center text-danger'>⚠️ Không có chi tiết hóa đơn</td></tr>");
                }

                // Lấy thông tin topping cho mỗi chi tiết
                ViewBag.OrderId = orderId;
                return PartialView("_ChiTietHoaDon", chiTiet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi ChiTietHoaDon: " + ex.Message);
                return Content("<tr class='detail-row'><td colspan='7' class='text-center text-danger'>⚠️ Lỗi server: " + ex.Message + "</td></tr>");
            }
        }

        // Action xuất chi tiết hóa đơn cho từng đơn (dùng cho modal)
        public ActionResult ExportOrderDetailToExcel(int orderId)
        {
            return ExportChiTietToExcel(orderId);
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
using System;
using System.Linq;
using System.Web.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    public class DonHangOnlineController : Controller
    {
        private WebAppDBEntities db = new WebAppDBEntities();

        public ActionResult Index()
        {
            TempData["ErrorMessage"] = null;

            // Lấy tất cả đơn hàng
            var allOrders = db.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToList()
                .Select(o =>
                {
                    var user = db.Users.FirstOrDefault(u => u.Id == o.UserId);
                    var status = db.OrderStatus.FirstOrDefault(s => s.StatusId == o.StatusId);
                    return new RecentOrderModel
                    {
                        OrderId = o.OrderId,
                        FullName = user != null ? user.FullName : "Unknown",
                        Status = status != null ? status.StatusName : "Unknown",
                        StatusId = o.StatusId,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount
                    };
                })
                .ToList();

            ViewBag.AllOrders = allOrders;
            ViewBag.OrderStatuses = db.OrderStatus.ToList();

            return View();
        }

        [HttpPost]
        public JsonResult UpdateOrderStatus(int orderId, int statusId)
        {
            try
            {
                var order = db.Orders.Find(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                var status = db.OrderStatus.FirstOrDefault(s => s.StatusId == statusId);
                if (status == null)
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ!" });
                }

                order.StatusId = statusId;
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!", newStatus = status.StatusName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetOrderDetail(int orderId)
        {
            try
            {
                // Lấy thông tin đơn hàng
                var order = (from o in db.Orders
                             join u in db.Users on o.UserId equals u.Id
                             join p in db.PhuongThucThanhToans on o.PaymentMethodId equals p.Id
                             join s in db.OrderStatus on o.StatusId equals s.StatusId
                             where o.OrderId == orderId
                             select new
                             {
                                 o.OrderId,
                                 u.FullName,
                                 u.Phone,
                                 u.Email,
                                 o.OrderDate,
                                 o.TotalAmount,
                                 o.DeliveryAddress,
                                 PaymentMethod = p.TenPhuongThuc,
                                 Status = s.StatusName,
                                 TotalAmountValue = o.TotalAmount
                             }).FirstOrDefault();

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" }, JsonRequestBehavior.AllowGet);
                }

                // Lấy chi tiết đơn hàng
                var orderDetails = (from od in db.OrderDetails
                                    join f in db.Foods on od.FoodId equals f.FoodId
                                    join s in db.Sizes on od.SizeId equals s.SizeID into sizeJoin
                                    from size in sizeJoin.DefaultIfEmpty()
                                    join t in db.Toppings on od.ToppingId equals t.ToppingID into toppingJoin
                                    from topping in toppingJoin.DefaultIfEmpty()
                                    where od.OrderId == orderId
                                    select new
                                    {
                                        od.OrderDetailId,
                                        FoodName = f.FoodName,
                                        SizeName = size != null ? size.SizeName : null,
                                        ToppingName = topping != null ? topping.ToppingName : null,
                                        od.Quantity,
                                        od.Price
                                    }).ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        order.OrderId,
                        order.FullName,
                        order.Phone,
                        order.Email,
                        OrderDate = order.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                        order.TotalAmount,
                        order.DeliveryAddress,
                        order.PaymentMethod,
                        order.Status,
                        order.TotalAmountValue,
                        OrderDetails = orderDetails
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
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
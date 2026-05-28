using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class GTController : Controller
    {
        // GET: GT
        private WebAppDBEntities db = new WebAppDBEntities();
        public ActionResult Index()
        {
            var invoiceData = db.InvoiceDetails
                .Where(x => x.FoodId != null)
                .Select(x => new
                {
                    FoodId = x.FoodId.Value,
                    Quantity = x.SoLuong
                });

            var orderData = db.OrderDetails
                .Select(x => new
                {
                    FoodId = x.FoodId,
                    Quantity = x.Quantity
                });

            var bestSellers = invoiceData
                .Union(orderData)
                .GroupBy(x => x.FoodId)
                .Select(g => new
                {
                    FoodId = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(8)
                .Join(db.Foods,
                    bs => bs.FoodId,
                    food => food.FoodId,
                    (bs, food) => new BanChayModel
                    {
                        FoodId = food.FoodId,
                        FoodName = food.FoodName,
                        ImageUrl = food.ImageURL,
                        Price = food.Price,
                        TotalSold = bs.TotalSold
                    })
                .ToList();

            ViewBag.BanChay = bestSellers;

            return View();
        }
    }
}
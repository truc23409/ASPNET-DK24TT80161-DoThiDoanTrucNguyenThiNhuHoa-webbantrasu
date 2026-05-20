using System.Collections.Generic;

namespace WebBanHang.Models
{
    public class ProductSales
    {
        public string FoodName { get; set; }
        public string ImageURL { get; set; } // Thêm thuộc tính này để lưu đường dẫn hình ảnh
        public List<MonthlySalesReport> SalesByMonth { get; set; }
        public int TotalSold { get; set; }
    }

    public class MonthlySalesReport
    {
        public int Month { get; set; }
        public int Quantity { get; set; }
    }
}
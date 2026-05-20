using System; // Thêm dòng này để import namespace System

namespace WebBanHang.Models
{
    public class RecentOrderModel
    {
        public int OrderId { get; set; }
        public string FullName { get; set; } // Đổi từ CustomerName thành FullName
        public string Status { get; set; }
        public int StatusId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
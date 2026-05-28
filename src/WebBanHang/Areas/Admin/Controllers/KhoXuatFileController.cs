using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ClosedXML.Excel;
using WebBanHang.Models;

public class KhoXuatFileController : Controller
{
    private WebAppDBEntities db = new WebAppDBEntities();

    // Hiển thị danh sách nguyên liệu
    public ActionResult Index()
    {
        var ingredients = db.Ingredients.ToList();
        ViewBag.Ingredients = ingredients;
        return View();
    }


    // Xuất danh sách nguyên liệu ra Excel
    public ActionResult ExportToExcel()
    {
        var ingredients = db.Ingredients.ToList();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Danh sách nguyên liệu");
            var currentRow = 1;

            // Header
            worksheet.Cell(currentRow, 1).Value = "STT";
            worksheet.Cell(currentRow, 2).Value = "Mã nguyên liệu";
            worksheet.Cell(currentRow, 3).Value = "Tên nguyên liệu";
            worksheet.Cell(currentRow, 4).Value = "Số lượng";
            worksheet.Cell(currentRow, 5).Value = "Hình ảnh";
            worksheet.Cell(currentRow, 6).Value = "Ngày cập nhật";

            // Định dạng header
            for (int col = 1; col <= 6; col++)
            {
                worksheet.Cell(currentRow, col).Style.Font.Bold = true;
                worksheet.Cell(currentRow, col).Style.Fill.BackgroundColor = XLColor.Black;
                worksheet.Cell(currentRow, col).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(currentRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Dữ liệu
            int index = 1;
            foreach (var item in ingredients)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = index++;
                worksheet.Cell(currentRow, 2).Value = item.IngredientId;
                worksheet.Cell(currentRow, 3).Value = item.IngredientName;
                worksheet.Cell(currentRow, 4).Value = item.SoLuong;
                worksheet.Cell(currentRow, 5).Value = item.ImageURL;
                worksheet.Cell(currentRow, 6).Value = item.LastUpdated.ToString("dd/MM/yyyy");
            }

            // Tự động điều chỉnh cột
            worksheet.Columns().AdjustToContents();

            // Lưu vào MemoryStream
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var fileName = "DanhSachNguyenLieu.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream.ToArray(), contentType, fileName);
            }
        }
    }
}

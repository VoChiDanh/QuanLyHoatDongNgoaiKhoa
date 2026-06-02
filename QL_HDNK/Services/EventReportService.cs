using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using QL_HDNK.Models;

namespace QL_HDNK.Services
{
    public class EventReportService
    {
        public byte[] CreateExcel(SuKien suKien, IEnumerable<DangKy_DiemDanh> registrations)
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Danh sách tham gia");
                sheet.Cell(1, 1).Value = "Sự kiện";
                sheet.Cell(1, 2).Value = suKien.TenSuKien;
                sheet.Cell(2, 1).Value = "Địa điểm";
                sheet.Cell(2, 2).Value = suKien.DiaDiem;
                sheet.Cell(3, 1).Value = "Thời gian";
                sheet.Cell(3, 2).Value = suKien.ThoiGianBatDau;
                sheet.Cell(3, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

                var row = 5;
                sheet.Cell(row, 1).Value = "STT";
                sheet.Cell(row, 2).Value = "Mã sinh viên";
                sheet.Cell(row, 3).Value = "Họ tên";
                sheet.Cell(row, 4).Value = "Email";
                sheet.Cell(row, 5).Value = "Đăng ký";
                sheet.Cell(row, 6).Value = "Điểm danh";
                sheet.Cell(row, 7).Value = "Ghi chú";
                sheet.Range(row, 1, row, 7).Style.Font.Bold = true;

                var index = 1;
                foreach (var item in registrations)
                {
                    row++;
                    sheet.Cell(row, 1).Value = index++;
                    sheet.Cell(row, 2).Value = item.NguoiDung != null ? item.NguoiDung.MaSinhVien : "";
                    sheet.Cell(row, 3).Value = item.NguoiDung != null ? item.NguoiDung.HoTen : "";
                    sheet.Cell(row, 4).Value = item.NguoiDung != null ? item.NguoiDung.Email : "";
                    sheet.Cell(row, 5).Value = item.TrangThaiDangKy != null ? item.TrangThaiDangKy.TenTTDK : "";
                    sheet.Cell(row, 6).Value = item.TrangThaiDiemDanh != null ? item.TrangThaiDiemDanh.TenTTDD : "";
                    sheet.Cell(row, 7).Value = item.GhiChu ?? "";
                }

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] CreatePdf(SuKien suKien, IList<DangKy_DiemDanh> registrations)
        {
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new WindowsFontResolver();
            }

            using (var stream = new MemoryStream())
            {
                var document = new PdfDocument();
                document.Info.Title = "Danh sách tham gia sự kiện";
                var page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                var gfx = XGraphics.FromPdfPage(page);
                var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
                var font = new XFont("Arial", 10, XFontStyleEx.Regular);
                var bold = new XFont("Arial", 10, XFontStyleEx.Bold);

                var y = 40d;
                gfx.DrawString("DANH SÁCH THAM GIA SỰ KIỆN", titleFont, XBrushes.Black, Rect(40, y, page.Width.Point - 80, 24), XStringFormats.TopCenter);
                y += 34;
                gfx.DrawString("Sự kiện: " + suKien.TenSuKien, bold, XBrushes.Black, 40d, y);
                y += 18;
                gfx.DrawString("Địa điểm: " + suKien.DiaDiem, font, XBrushes.Black, 40d, y);
                y += 18;
                gfx.DrawString("Thời gian: " + suKien.ThoiGianBatDau.ToString("dd/MM/yyyy HH:mm"), font, XBrushes.Black, 40d, y);
                y += 30;

                gfx.DrawString("STT", bold, XBrushes.Black, 40d, y);
                gfx.DrawString("Mã SV", bold, XBrushes.Black, 80d, y);
                gfx.DrawString("Họ tên", bold, XBrushes.Black, 150d, y);
                gfx.DrawString("Email", bold, XBrushes.Black, 300d, y);
                y += 16;

                for (var i = 0; i < registrations.Count; i++)
                {
                    if (y > page.Height.Point - 40)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 40;
                    }

                    var user = registrations[i].NguoiDung;
                    gfx.DrawString((i + 1).ToString(), font, XBrushes.Black, 40d, y);
                    gfx.DrawString(user != null ? user.MaSinhVien : "", font, XBrushes.Black, 80d, y);
                    gfx.DrawString(user != null ? user.HoTen : "", font, XBrushes.Black, 150d, y);
                    gfx.DrawString(user != null ? user.Email : "", font, XBrushes.Black, 300d, y);
                    y += 16;
                }

                document.Save(stream, false);
                return stream.ToArray();
            }
        }

        private static XRect Rect(double x, double y, double width, double height)
        {
            return new XRect(x, y, width, height);
        }
    }
}

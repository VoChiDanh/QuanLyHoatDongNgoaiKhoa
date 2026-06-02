using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using QL_HDNK.Models;

namespace QL_HDNK.Services
{
    public class ReportService
    {
        #region Faculty Report

        public byte[] CreateFacultyExcel(FacultyReportViewModel report)
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("BM_KXBCK");
                var lastCol = Math.Max(2, 1 + report.Rows.Count);

                sheet.Cell(1, 1).Value = "BÁO CÁO THAM GIA HOẠT ĐỘNG TOÀN KHOA";
                sheet.Range(1, 1, 1, lastCol).Merge();
                sheet.Row(1).Height = 28;
                sheet.Cell(1, 1).Style.Font.Bold = true;
                sheet.Cell(1, 1).Style.Font.FontSize = 16;
                sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
                sheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#17324d");
                sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                sheet.Cell(2, 1).Value = "Khoa";
                sheet.Cell(2, 2).Value = report.TenKhoa ?? report.MaKhoa;
                sheet.Cell(3, 1).Value = "Chế độ";
                sheet.Cell(3, 2).Value = report.CheDo == "phan-tram" ? "% tham gia" : "Số lượng tham gia";
                sheet.Cell(2, 4).Value = "Lớp";
                sheet.Cell(2, 5).Value = report.TotalClasses;
                sheet.Cell(3, 4).Value = "Sinh viên";
                sheet.Cell(3, 5).Value = report.TotalStudents;
                sheet.Range(2, 1, 3, 5).Style.Font.Bold = true;

                var headerRow = 5;
                sheet.Cell(headerRow, 1).Value = "Sự kiện / Thời gian";

                var col = 2;
                foreach (var lop in report.Rows)
                {
                    sheet.Cell(headerRow, col).Value = lop.TenLop + Environment.NewLine + "(" + lop.KhoaHoc + ")";
                    col++;
                }

                var header = sheet.Range(headerRow, 1, headerRow, Math.Max(2, col - 1));
                header.Style.Font.Bold = true;
                header.Style.Font.FontColor = XLColor.White;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#24536f");
                header.Style.Alignment.WrapText = true;
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Row(headerRow).Height = 40;

                var row = headerRow;
                foreach (var ev in report.Events)
                {
                    row++;
                    sheet.Cell(row, 1).Value = ev.TenSuKien + Environment.NewLine + "(" + ev.ThoiGianText + ")";
                    sheet.Cell(row, 1).Style.Alignment.WrapText = true;
                    sheet.Cell(row, 1).Style.Font.Bold = true;

                    var c = 2;
                    foreach (var lop in report.Rows)
                    {
                        var cellData = lop.Cells.FirstOrDefault(x => x.MaSuKien == ev.MaSuKien);
                        if (cellData != null)
                        {
                            if (report.CheDo == "phan-tram")
                            {
                                sheet.Cell(row, c).Value = cellData.TyLeThamGia / 100m;
                                sheet.Cell(row, c).Style.NumberFormat.Format = "0.0%";
                            }
                            else
                            {
                                sheet.Cell(row, c).Value = cellData.SoLuongThamGia;
                            }
                            sheet.Cell(row, c).Style.Fill.BackgroundColor = HeatColor(cellData.SoLuongThamGia, cellData.SiSo);
                        }
                        c++;
                    }
                }

                var table = sheet.Range(headerRow, 1, Math.Max(headerRow, row), Math.Max(2, col - 1));
                table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                table.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Range(headerRow + 1, 2, Math.Max(headerRow + 1, row), Math.Max(2, col - 1)).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                sheet.Column(1).Width = 35;
                for (var i = 2; i <= Math.Max(2, col - 1); i++) sheet.Column(i).Width = 15;

                sheet.SheetView.FreezeRows(headerRow);
                sheet.SheetView.FreezeColumns(1);

                using (var ms = new MemoryStream()) { workbook.SaveAs(ms); return ms.ToArray(); }
            }
        }

        public byte[] CreateFacultyPdf(FacultyReportViewModel report)
        {
            EnsureFontResolver();
            using (var stream = new MemoryStream())
            {
                var document = new PdfDocument();
                document.Info.Title = "Báo cáo tham gia hoạt động toàn khoa";

                if (report.Rows == null || !report.Rows.Any())
                {
                    var page = document.AddPage();
                    var gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawString("Không có dữ liệu báo cáo.", new XFont("Arial", 12, XFontStyleEx.Regular), XBrushes.Black, new XPoint(40, 40));
                }
                else
                {
                    const int classesPerPage = 6;
                    var classCount = report.Rows.Count;
                    for (var start = 0; start < classCount; start += classesPerPage)
                    {
                        DrawFacultyPdfPage(document, report, start, Math.Min(classesPerPage, classCount - start));
                    }
                }

                document.Save(stream, false);
                return stream.ToArray();
            }
        }

        private void DrawFacultyPdfPage(PdfDocument document, FacultyReportViewModel report, int start, int take)
        {
            var page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            var gfx = XGraphics.FromPdfPage(page);
            var titleFont = new XFont("Arial", 14, XFontStyleEx.Bold);
            var font = new XFont("Arial", 8, XFontStyleEx.Regular);
            var bold = new XFont("Arial", 8, XFontStyleEx.Bold);
            var margin = 28d;
            var y = 24d;
            var pageWidth = page.Width.Point;
            var pageHeight = page.Height.Point;

            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(23, 50, 77)), margin, y, pageWidth - margin * 2, 34d);
            gfx.DrawString("BÁO CÁO THAM GIA HOẠT ĐỘNG TOÀN KHOA", titleFont, XBrushes.White, Rect(margin, y + 8, pageWidth - margin * 2, 18), XStringFormats.TopCenter);
            y += 44;
            gfx.DrawString("Khoa: " + (report.TenKhoa ?? report.MaKhoa), bold, XBrushes.Black, margin, y);
            gfx.DrawString("Chế độ: " + (report.CheDo == "phan-tram" ? "% tham gia" : "Số lượng tham gia"), bold, XBrushes.Black, margin + 260, y);
            gfx.DrawString("Trang lớp: " + (start + 1) + "-" + Math.Min(start + take, report.Rows.Count) + "/" + report.Rows.Count, bold, XBrushes.Black, margin + 520, y);
            y += 18;

            var eventColWidth = 180d;
            var usable = pageWidth - margin * 2 - eventColWidth;
            var colWidth = take == 0 ? usable : usable / take;
            var rowHeight = 54d;
            var headerHeight = 40d;
            var x = margin;

            DrawHeaderCell(gfx, bold, "Sự kiện / Thời gian", x, y, eventColWidth, headerHeight); x += eventColWidth;
            var pageClasses = report.Rows.Skip(start).Take(take).ToList();
            foreach (var lop in pageClasses)
            {
                DrawHeaderCell(gfx, bold, lop.TenLop + "\n(" + lop.KhoaHoc + ")", x, y, colWidth, headerHeight);
                x += colWidth;
            }

            y += headerHeight;
            foreach (var ev in report.Events)
            {
                if (y + rowHeight > pageHeight - 24)
                {
                    page = document.AddPage();
                    page.Orientation = PdfSharp.PageOrientation.Landscape;
                    gfx = XGraphics.FromPdfPage(page);
                    y = 24; x = margin;
                    DrawHeaderCell(gfx, bold, "Sự kiện / Thời gian", x, y, eventColWidth, headerHeight); x += eventColWidth;
                    foreach (var lop in pageClasses) { DrawHeaderCell(gfx, bold, lop.TenLop + "\n(" + lop.KhoaHoc + ")", x, y, colWidth, headerHeight); x += colWidth; }
                    y += headerHeight;
                }

                x = margin;
                DrawCell(gfx, font, ev.TenSuKien + "\n(" + ev.ThoiGianText + ")", x, y, eventColWidth, rowHeight, false); x += eventColWidth;
                foreach (var row in pageClasses)
                {
                    var cell = row.Cells.FirstOrDefault(c => c.MaSuKien == ev.MaSuKien);
                    if (cell != null)
                    {
                        var val = report.CheDo == "phan-tram" ? cell.TyLeThamGia.ToString("0.0") + "%" : cell.SoLuongThamGia.ToString();
                        DrawCell(gfx, bold, val, x, y, colWidth, rowHeight, true, PdfHeatColor(cell.SoLuongThamGia, cell.SiSo));
                    }
                    else DrawCell(gfx, font, "-", x, y, colWidth, rowHeight, true);
                    x += colWidth;
                }
                y += rowHeight;
            }
        }

        #endregion

        #region Class Report

        public byte[] CreateClassExcel(ClassReportViewModel report)
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Thống kê lớp");

                // Tính toán các chỉ số tổng quan
                int totalEvents = report.Events.Count;
                int totalStudents = report.TotalStudents;
                int totalPossible = totalStudents * totalEvents;
                int totalAttended = report.Rows != null ? report.Rows.Sum(r => r.Cells.Count(c => c.CoMat)) : 0;
                double avgParticipation = totalPossible > 0 ? Math.Round((double)totalAttended / totalPossible * 100, 1) : 0;

                var lastCol = Math.Max(3, 3 + totalEvents);

                // 1. Tiêu đề
                sheet.Cell(1, 1).Value = "THỐNG KÊ THAM GIA HOẠT ĐỘNG LỚP " + report.TenLop?.ToUpper();
                sheet.Range(1, 1, 1, lastCol).Merge();
                sheet.Row(1).Height = 28;
                sheet.Cell(1, 1).Style.Font.Bold = true;
                sheet.Cell(1, 1).Style.Font.FontSize = 16;
                sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
                sheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#17324d");
                sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // 2. Thông tin chung & Thống kê
                sheet.Cell(2, 1).Value = "Khoa:";
                sheet.Cell(2, 2).Value = report.TenKhoa;
                sheet.Cell(2, 4).Value = "Lớp:";
                sheet.Cell(2, 5).Value = report.TenLop;

                sheet.Cell(3, 1).Value = "Tổng sinh viên:";
                sheet.Cell(3, 2).Value = totalStudents;
                sheet.Cell(3, 4).Value = "Tổng số sự kiện:";
                sheet.Cell(3, 5).Value = totalEvents;

                sheet.Cell(4, 1).Value = "Tỷ lệ tham gia TB:";
                sheet.Cell(4, 2).Value = $"{avgParticipation}%";

                sheet.Range(2, 1, 4, 1).Style.Font.Bold = true;
                sheet.Range(2, 4, 3, 4).Style.Font.Bold = true;

                sheet.Cell(3, 2).Style.Font.FontColor = XLColor.FromHtml("#0d6efd");
                sheet.Cell(3, 2).Style.Font.Bold = true;
                sheet.Cell(3, 5).Style.Font.FontColor = XLColor.FromHtml("#198754");
                sheet.Cell(3, 5).Style.Font.Bold = true;
                sheet.Cell(4, 2).Style.Font.FontColor = XLColor.FromHtml("#0dcaf0");
                sheet.Cell(4, 2).Style.Font.Bold = true;

                // 3. Tiêu đề cột (Headers)
                var headerRow = 6;
                sheet.Cell(headerRow, 1).Value = "Sinh viên / Mã SV";
                sheet.Cell(headerRow, 2).Value = "Tổng Tham Gia";
                sheet.Cell(headerRow, 3).Value = "Tỷ lệ (%)";

                var col = 4;
                foreach (var ev in report.Events)
                {
                    sheet.Cell(headerRow, col).Value = ev.TenSuKien + Environment.NewLine + "(" + ev.ThoiGianText + ")";
                    col++;
                }

                var header = sheet.Range(headerRow, 1, headerRow, lastCol);
                header.Style.Font.Bold = true;
                header.Style.Font.FontColor = XLColor.White;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#24536f");
                header.Style.Alignment.WrapText = true;
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Row(headerRow).Height = 40;

                // 4. Đổ dữ liệu
                var row = headerRow;
                foreach (var sv in report.Rows)
                {
                    row++;
                    int attended = sv.Cells.Count(e => e.CoMat);
                    double percent = totalEvents > 0 ? Math.Round((double)attended / totalEvents * 100, 1) : 0;

                    // Thông tin sinh viên
                    sheet.Cell(row, 1).Value = sv.HoTen + Environment.NewLine + sv.MaSinhVien;
                    sheet.Cell(row, 1).Style.Alignment.WrapText = true;

                    // Thống kê tổng
                    sheet.Cell(row, 2).Value = $"{attended} / {totalEvents}";

                    // Tỷ lệ %
                    sheet.Cell(row, 3).Value = percent / 100.0;
                    sheet.Cell(row, 3).Style.NumberFormat.Format = "0.0%";
                    sheet.Cell(row, 3).Style.Font.Bold = true;
                    if (percent >= 80) sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#198754");
                    else if (percent >= 50) sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#ffc107");
                    else sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#dc3545");

                    var c = 4;
                    foreach (var ev in report.Events)
                    {
                        var cellData = sv.Cells.FirstOrDefault(x => x.MaSuKien == ev.MaSuKien);
                        if (cellData != null && cellData.CoMat)
                        {
                            sheet.Cell(row, c).Value = cellData.Diem > 0 ? $"✔ (+{cellData.Diem})" : "✔";
                            sheet.Cell(row, c).Style.Font.FontColor = XLColor.FromHtml("#198754");
                            sheet.Cell(row, c).Style.Font.Bold = true;
                            sheet.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#d8f3dc");
                        }
                        else
                        {
                            sheet.Cell(row, c).Value = "-";
                            sheet.Cell(row, c).Style.Font.FontColor = XLColor.Gray;
                        }
                        c++;
                    }
                }

                // 5. Kẻ bảng và căn lề
                var table = sheet.Range(headerRow, 1, Math.Max(headerRow, row), lastCol);
                table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                table.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Range(headerRow + 1, 2, Math.Max(headerRow + 1, row), lastCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                sheet.Column(1).Width = 35;
                sheet.Column(2).Width = 15;
                sheet.Column(3).Width = 12;
                for (var i = 4; i <= lastCol; i++) sheet.Column(i).Width = 18;

                sheet.SheetView.FreezeRows(headerRow);
                sheet.SheetView.FreezeColumns(1);

                using (var ms = new MemoryStream()) { workbook.SaveAs(ms); return ms.ToArray(); }
            }
        }

        public byte[] CreateClassPdf(ClassReportViewModel report)
        {
            EnsureFontResolver();
            using (var stream = new MemoryStream())
            {
                var document = new PdfDocument();
                document.Info.Title = "Thống kê tham gia hoạt động lớp";

                if (report.Rows == null || !report.Rows.Any())
                {
                    var page = document.AddPage();
                    var gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawString("Không có dữ liệu báo cáo.", new XFont("Arial", 12, XFontStyleEx.Regular), XBrushes.Black, new XPoint(40, 40));
                }
                else
                {
                    // Tính toán các chỉ số cho PDF
                    int totalEvents = report.Events.Count;
                    int totalStudents = report.TotalStudents;
                    int totalPossible = totalStudents * totalEvents;
                    int totalAttended = report.Rows != null ? report.Rows.Sum(r => r.Cells.Count(c => c.CoMat)) : 0;
                    double avgParticipation = totalPossible > 0 ? Math.Round((double)totalAttended / totalPossible * 100, 1) : 0;

                    const int studentsPerPage = 12;
                    var studentCount = report.Rows.Count;
                    for (var start = 0; start < studentCount; start += studentsPerPage)
                    {
                        DrawClassPdfPage(document, report, start, Math.Min(studentsPerPage, studentCount - start), totalStudents, totalEvents, avgParticipation);
                    }
                }

                document.Save(stream, false);
                return stream.ToArray();
            }
        }

        private void DrawClassPdfPage(PdfDocument document, ClassReportViewModel report, int start, int take, int totalStudents, int totalEvents, double avgParticipation)
        {
            var page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            var gfx = XGraphics.FromPdfPage(page);
            var titleFont = new XFont("Arial", 14, XFontStyleEx.Bold);
            var font = new XFont("Arial", 8, XFontStyleEx.Regular);
            var bold = new XFont("Arial", 8, XFontStyleEx.Bold);
            var margin = 28d;
            var y = 24d;
            var pageWidth = page.Width.Point;
            var pageHeight = page.Height.Point;

            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(23, 50, 77)), margin, y, pageWidth - margin * 2, 34d);
            gfx.DrawString("THỐNG KÊ THAM GIA HOẠT ĐỘNG LỚP " + (report.TenLop?.ToUpper() ?? ""), titleFont, XBrushes.White, Rect(margin, y + 8, pageWidth - margin * 2, 18), XStringFormats.TopCenter);
            y += 44;

            // Dòng thông tin 1
            gfx.DrawString("Khoa: " + (report.TenKhoa ?? ""), bold, XBrushes.Black, margin, y);
            gfx.DrawString("Lớp: " + (report.TenLop ?? ""), bold, XBrushes.Black, margin + 260, y);
            gfx.DrawString("Trang SV: " + (start + 1) + "-" + Math.Min(start + take, report.Rows.Count) + "/" + report.Rows.Count, bold, XBrushes.Black, margin + 520, y);
            y += 16;

            // Dòng thông tin 2
            gfx.DrawString($"Tổng sinh viên: {totalStudents}", bold, XBrushes.DarkBlue, margin, y);
            gfx.DrawString($"Tổng sự kiện: {totalEvents}", bold, XBrushes.DarkGreen, margin + 260, y);
            gfx.DrawString($"Tỷ lệ tham gia TB: {avgParticipation}%", bold, XBrushes.DarkCyan, margin + 520, y);
            y += 20;

            var svColWidth = 130d;
            var totalColWidth = 50d;
            var percentColWidth = 45d;
            var usable = pageWidth - margin * 2 - svColWidth - totalColWidth - percentColWidth;
            var evColWidth = report.Events.Count == 0 ? usable : usable / report.Events.Count;
            var headerHeight = 40d;
            var rowHeight = 32d;
            var x = margin;

            DrawHeaderCell(gfx, bold, "Sinh viên / Mã SV", x, y, svColWidth, headerHeight); x += svColWidth;
            DrawHeaderCell(gfx, bold, "Tổng\nTham Gia", x, y, totalColWidth, headerHeight); x += totalColWidth;
            DrawHeaderCell(gfx, bold, "Tỷ lệ\n(%)", x, y, percentColWidth, headerHeight); x += percentColWidth;

            foreach (var ev in report.Events)
            {
                DrawHeaderCell(gfx, bold, ev.TenSuKien + "\n(" + ev.ThoiGianText + ")", x, y, evColWidth, headerHeight);
                x += evColWidth;
            }

            y += headerHeight;

            var pageStudents = report.Rows.Skip(start).Take(take).ToList();
            foreach (var sv in pageStudents)
            {
                x = margin;
                int attended = sv.Cells.Count(c => c.CoMat);
                double percent = report.Events.Count > 0 ? Math.Round((double)attended / report.Events.Count * 100, 1) : 0;

                DrawCell(gfx, font, sv.HoTen + "\n" + sv.MaSinhVien, x, y, svColWidth, rowHeight, false); x += svColWidth;
                DrawCell(gfx, bold, $"{attended} / {report.Events.Count}", x, y, totalColWidth, rowHeight, true); x += totalColWidth;

                XBrush percentBrush = XBrushes.Red;
                if (percent >= 80) percentBrush = XBrushes.Green;
                else if (percent >= 50) percentBrush = XBrushes.Orange;

                DrawCell(gfx, bold, $"{percent}%", x, y, percentColWidth, rowHeight, true, null, percentBrush); x += percentColWidth;

                foreach (var ev in report.Events)
                {
                    var cell = sv.Cells.FirstOrDefault(c => c.MaSuKien == ev.MaSuKien);
                    if (cell != null && cell.CoMat)
                    {
                        var val = cell.Diem > 0 ? $"V (+{cell.Diem})" : "V";
                        DrawCell(gfx, bold, val, x, y, evColWidth, rowHeight, true, XColor.FromArgb(216, 243, 220), XBrushes.DarkGreen);
                    }
                    else
                    {
                        DrawCell(gfx, font, "-", x, y, evColWidth, rowHeight, true, null, XBrushes.Gray);
                    }
                    x += evColWidth;
                }
                y += rowHeight;
            }
        }

        #endregion

        #region Helpers

        private void EnsureFontResolver() { if (GlobalFontSettings.FontResolver == null) GlobalFontSettings.FontResolver = new WindowsFontResolver(); }

        private static void DrawHeaderCell(XGraphics gfx, XFont font, string text, double x, double y, double width, double height)
        {
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(36, 83, 111)), x, y, width, height);
            gfx.DrawRectangle(new XPen(XColor.FromArgb(180, 196, 208), 0.6), x, y, width, height);
            var lines = WrapText(gfx, text ?? "", font, width - 6);
            for (var i = 0; i < lines.Count; i++)
            {
                var lineY = y + 5 + i * 11;
                if (lineY + 10 > y + height) break;
                gfx.DrawString(lines[i], font, XBrushes.White, Rect(x + 3, lineY, width - 6, 12), XStringFormats.TopCenter);
            }
        }

        private static void DrawCell(XGraphics gfx, XFont font, string text, double x, double y, double width, double height, bool center, XColor? fill = null, XBrush textBrush = null)
        {
            var brush = textBrush ?? XBrushes.Black;
            gfx.DrawRectangle(new XSolidBrush(fill ?? XColor.FromArgb(255, 255, 255)), x, y, width, height);
            gfx.DrawRectangle(new XPen(XColor.FromArgb(216, 225, 232), 0.5), x, y, width, height);
            var lines = WrapText(gfx, text ?? "", font, width - 6);
            var format = center ? XStringFormats.TopCenter : XStringFormats.TopLeft;
            for (var i = 0; i < lines.Count; i++)
            {
                var lineY = y + 5 + i * 11;
                if (lineY + 10 > y + height) break;
                gfx.DrawString(lines[i], font, brush, Rect(x + 3, lineY, width - 6, 12), format);
            }
        }

        private static List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            var result = new List<string>();
            var paragraphs = text.Split('\n');
            foreach (var p in paragraphs)
            {
                var words = p.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) { result.Add(""); continue; }
                var currentLine = "";
                foreach (var word in words)
                {
                    var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    if (gfx.MeasureString(testLine, font).Width > maxWidth && !string.IsNullOrEmpty(currentLine)) { result.Add(currentLine); currentLine = word; }
                    else currentLine = testLine;
                }
                if (!string.IsNullOrEmpty(currentLine)) result.Add(currentLine);
            }
            return result;
        }

        private static XRect Rect(double x, double y, double width, double height) => new XRect(x, y, width, height);

        private static XLColor HeatColor(int val, int total)
        {
            var ratio = total == 0 ? 0 : (double)val / total;
            if (ratio >= 0.75) return XLColor.FromHtml("#d8f3dc");
            if (ratio >= 0.5) return XLColor.FromHtml("#fff3bf");
            if (ratio > 0) return XLColor.FromHtml("#ffe3d5");
            return XLColor.FromHtml("#f5f7fa");
        }

        private static XColor PdfHeatColor(int val, int total)
        {
            var ratio = total == 0 ? 0 : (double)val / total;
            if (ratio >= 0.75) return XColor.FromArgb(216, 243, 220);
            if (ratio >= 0.5) return XColor.FromArgb(255, 243, 191);
            if (ratio > 0) return XColor.FromArgb(255, 227, 213);
            return XColor.FromArgb(245, 247, 250);
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using QL_HDNK.Models; // Đảm bảo đã có namespace này để nhận diện DiemRenLuyenViewModel

namespace QL_HDNK.Services
{
    public class PersonalReportService
    {
        // THAY ĐỔI: Tham số cuối cùng đổi từ List<DangKy_DiemDanh> thành DiemRenLuyenViewModel data
        public byte[] CreatePersonalPdf(NguoiDung student, HocKy hocKy, NamHoc namHoc, DiemRenLuyenViewModel data)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Khởi tạo tài liệu khổ A4, lề 30px
                var document = new Document(PageSize.A4, 30, 30, 30, 30);
                var writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                // Cấu hình Font chữ hệ thống hiển thị tiếng Việt Unicode
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Arial.ttf");
                var bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                // CÁC FONT CHỮ DÙNG CHUNG (Mặc định màu đen)
                var titleFont = new Font(bf, 16, Font.BOLD, BaseColor.BLACK);
                var headerFont = new Font(bf, 11, Font.BOLD, BaseColor.BLACK);
                var normalFont = new Font(bf, 11, Font.NORMAL, BaseColor.BLACK);
                var boldFont = new Font(bf, 11, Font.BOLD, BaseColor.BLACK);

                // THÊM MỚI: CÁC FONT CHỮ ĐƯỢC TÔ MÀU SẴN (Không được thay đổi màu của chúng trong vòng lặp)
                var redBoldFont = new Font(bf, 11, Font.BOLD, new BaseColor(220, 53, 69));
                var greenBoldFont = new Font(bf, 11, Font.BOLD, new BaseColor(25, 135, 84));
                var greenNormalFont = new Font(bf, 11, Font.NORMAL, new BaseColor(25, 135, 84));

                // 1. Vẽ tiêu đề biểu mẫu
                var title = new Paragraph("PHIẾU THỐNG KÊ HOẠT ĐỘNG NGOẠI KHÓA", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20;
                document.Add(title);

                // 2. Điền thông tin hành chính của sinh viên
                document.Add(new Paragraph($"Họ và tên sinh viên: {student.HoTen}", normalFont));
                document.Add(new Paragraph($"Mã số sinh viên: {student.MaSinhVien}", normalFont));
                document.Add(new Paragraph($"Lớp: {student.LopHoc?.TenLop ?? "Chưa xếp lớp"}", normalFont));
                document.Add(new Paragraph($"Học kỳ: {hocKy?.TenHocKy ?? "N/A"} | Năm học: {namHoc?.TenNamHoc ?? "N/A"}", normalFont));

                var space = new Paragraph(" ");
                space.SpacingAfter = 15;
                document.Add(space);

                // 3. Khởi tạo bảng danh sách hoạt động (4 cột)
                var table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 8f, 37f, 25f, 15f, 15f }); // Tỷ lệ độ rộng các cột

                // Định dạng tiêu đề các cột (Header)
                table.AddCell(new PdfPCell(new Phrase("STT", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(240, 240, 240), Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Tên Sự Kiện", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(240, 240, 240), Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Mức Điểm Áp Dụng", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(240, 240, 240), Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Trạng Thái", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(240, 240, 240), Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Điểm", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(240, 240, 240), Padding = 6 });
                // 4. Duyệt cấu trúc lồng nhau (Nested Loop) qua từng danh mục để lấy trọn vẹn sự kiện
                // 4. Vòng lặp đổ dữ liệu sự kiện
                int stt = 1;
                if (data != null && data.DanhMucs != null)
                {
                    foreach (var danhMuc in data.DanhMucs)
                    {
                        foreach (var item in danhMuc.ChiTiets)
                        {
                            table.AddCell(new PdfPCell(new Phrase(stt.ToString(), normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(item.TenSuKien ?? "", normalFont)) { Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(item.TenMucDiem ?? "", normalFont)) { Padding = 5 });

                            // --- XỬ LÝ CỘT TRẠNG THÁI ---
                            string strTrangThai = item.DaThamGia ? "Có mặt" : "Vắng mặt";

                            // Gán trực tiếp Font đã tô màu sẵn, KHÔNG DÙNG lệnh đổi màu .Color nữa
                            Font statusFont = item.DaThamGia ? greenNormalFont : redBoldFont;

                            var statusCell = new PdfPCell(new Phrase(strTrangThai, statusFont));
                            statusCell.HorizontalAlignment = Element.ALIGN_CENTER;
                            statusCell.Padding = 5;
                            table.AddCell(statusCell);


                            // --- XỬ LÝ CỘT ĐIỂM CỘNG/TRƯ ---
                            string diemStr = item.DiemCongTrangThai > 0 ? $"+{item.DiemCongTrangThai}" : item.DiemCongTrangThai.ToString();

                            // Chọn Font màu dựa trên số điểm
                            Font pointFont = boldFont; // Mặc định dùng font Đen cho trường hợp bằng 0
                            if (item.DiemCongTrangThai >= 0)
                            {
                                pointFont = greenBoldFont; // Nhận màu Xanh
                            }
                            else if (item.DiemCongTrangThai < 0)
                            {
                                pointFont = redBoldFont; // Nhận màu Đỏ
                            }

                            var pointCell = new PdfPCell(new Phrase(diemStr, pointFont));
                            pointCell.HorizontalAlignment = Element.ALIGN_CENTER;
                            pointCell.Padding = 5;
                            table.AddCell(pointCell);

                            stt++;
                        }
                    }
                }

                document.Add(table);
                document.Close();

                return memoryStream.ToArray();
            }
        }
    }
}
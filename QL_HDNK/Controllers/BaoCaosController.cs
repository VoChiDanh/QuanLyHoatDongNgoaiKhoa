using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using QL_HDNK.Filters;
using QL_HDNK.Models;
using QL_HDNK.Services;

namespace QL_HDNK.Controllers
{
    /// <summary>
    /// Controller xử lý các yêu cầu liên quan đến Báo cáo và Thống kê.
    /// Cho phép xem báo cáo tham gia hoạt động theo Khoa hoặc theo Lớp, và xuất file Excel/PDF.
    /// </summary>
    [Authorize]
    [RoleAuthorize(RoleKeys.Admin, RoleKeys.FacultyUnion, RoleKeys.ClassOfficer)]
    public class BaoCaosController : Controller
    {
        private readonly QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();
        private readonly ReportService reportService = new ReportService();

        /// <summary>
        /// Hiển thị trang báo cáo thống kê cho toàn Khoa.
        /// </summary>
        /// <param name="maKhoa">Mã khoa cần xem báo cáo</param>
        /// <param name="maNamHoc">Mã năm học lọc dữ liệu</param>
        /// <param name="maHocKy">Mã học kỳ lọc dữ liệu</param>
        /// <param name="cheDo">Chế độ hiển thị (so-luong hoặc ty-le)</param>
        /// <returns>View với dữ liệu báo cáo cấp Khoa</returns>
        public ActionResult ToanKhoa(string maKhoa, string maNamHoc, string maHocKy, string cheDo = "so-luong")
        {
            var userRole = CurrentRole();
            var userFacultyId = CurrentFacultyId();

            // Nếu là BCH Khoa, cố định mã khoa theo khoa của người dùng
            if (userRole == RoleKeys.FacultyUnion) maKhoa = userFacultyId;
            
            // Thiết lập giá trị mặc định nếu chưa chọn
            if (string.IsNullOrEmpty(maKhoa) && userRole == RoleKeys.Admin)
            {
                maKhoa = db.Khoas.Select(k => k.MaKhoa).FirstOrDefault();
            }

            if (string.IsNullOrEmpty(maNamHoc))
            {
                var now = DateTime.Now;
                maNamHoc = db.NamHocs.Where(n => n.TuNgay <= now && n.DenNgay >= now).Select(n => n.MaNamHoc).FirstOrDefault()
                          ?? db.NamHocs.OrderByDescending(n => n.MaNamHoc).Select(n => n.MaNamHoc).FirstOrDefault();
            }

            if (string.IsNullOrEmpty(maHocKy))
            {
                var now = DateTime.Now;
                maHocKy = db.HocKies.Where(h => h.TuNgay <= now && h.DenNgay >= now).Select(h => h.MaHocKy).FirstOrDefault()
                          ?? db.HocKies.OrderByDescending(h => h.MaHocKy).Select(h => h.MaHocKy).FirstOrDefault();
            }

            var model = new FacultyReportViewModel
            {
                MaKhoa = maKhoa,
                MaNamHoc = maNamHoc,
                MaHocKy = maHocKy,
                CheDo = cheDo,
                Events = new List<FacultyReportEventHeader>(),
                Rows = new List<FacultyReportRow>()
            };

            LoadFilters(model, userRole, userFacultyId);

            if (!string.IsNullOrEmpty(maKhoa))
            {
                CalculateFacultyData(model);
            }

            return View(model);
        }

        /// <summary>
        /// Hiển thị trang báo cáo thống kê chi tiết cho một Lớp.
        /// </summary>
        /// <param name="maKhoa">Mã khoa của lớp</param>
        /// <param name="maLop">Mã lớp cần xem báo cáo</param>
        /// <param name="maNamHoc">Mã năm học lọc dữ liệu</param>
        /// <param name="maHocKy">Mã học kỳ lọc dữ liệu</param>
        /// <returns>View với dữ liệu báo cáo cấp Lớp</returns>
        public ActionResult ThongKeLop(string maKhoa, string maLop, string maNamHoc, string maHocKy)
        {
            var userRole = CurrentRole();
            var userFacultyId = CurrentFacultyId();
            var userClassId = CurrentClassId();

            if (userRole == RoleKeys.FacultyUnion) maKhoa = userFacultyId;
            if (userRole == RoleKeys.ClassOfficer) 
            { 
                maKhoa = userFacultyId; 
                maLop = userClassId; 
            }
            // set mặc định khoa, lớp
            if (string.IsNullOrEmpty(maKhoa) && userRole == RoleKeys.Admin)
            {
                maKhoa = db.Khoas.Select(k => k.MaKhoa).FirstOrDefault();
            }

            if (string.IsNullOrEmpty(maLop) && !string.IsNullOrEmpty(maKhoa))
            {
                maLop = db.LopHocs.Where(l => l.MaKhoa == maKhoa).Select(l => l.MaLop).FirstOrDefault();
            }
            //set mốc thời gian hiện tại
            if (string.IsNullOrEmpty(maNamHoc))
            {
                var now = DateTime.Now;
                maNamHoc = db.NamHocs.Where(n => n.TuNgay <= now && n.DenNgay >= now).Select(n => n.MaNamHoc).FirstOrDefault()
                          ?? db.NamHocs.OrderByDescending(n => n.MaNamHoc).Select(n => n.MaNamHoc).FirstOrDefault();
            }
            if (string.IsNullOrEmpty(maHocKy))
            {
                var now = DateTime.Now;
                maHocKy = db.HocKies.Where(h => h.TuNgay <= now && h.DenNgay >= now).Select(h => h.MaHocKy).FirstOrDefault()
                          ?? db.HocKies.OrderByDescending(h => h.MaHocKy).Select(h => h.MaHocKy).FirstOrDefault();
            }

            var model = new ClassReportViewModel
            {
                MaKhoa = maKhoa,
                MaLop = maLop,
                MaNamHoc = maNamHoc,
                MaHocKy = maHocKy,
                Events = new List<ClassReportEventHeader>(),
                Rows = new List<ClassReportRow>()
            };
            // Nạp các danh sách lọc (Khoa, Lớp, Năm học, Học kỳ) cho báo cáo Lớp.
            LoadClassFilters(model, userRole, userFacultyId, userClassId);

            if (!string.IsNullOrEmpty(maLop))
            {
                CalculateClassData(model);
            }

            return View(model);
        }

        /// <summary>
        /// Xuất dữ liệu báo cáo cấp Khoa ra file Excel hoặc PDF.
        /// </summary>
        /// <param name="model">Dữ liệu lọc báo cáo</param>
        /// <param name="format">Định dạng file xuất (excel hoặc pdf)</param>
        /// <returns>File dữ liệu kết quả</returns>
        [HttpPost]
        public ActionResult ExportFaculty(FacultyReportViewModel model, string format)
        {
            string fileName = $"BaoCaoKhoa_{model.MaKhoa}_{DateTime.Now:yyyyMMdd}";

            try {
                CalculateFacultyData(model);
                byte[] fileContent;
                if (format == "pdf")
                {
                    fileContent = reportService.CreateFacultyPdf(model);
                    return File(fileContent, "application/pdf", fileName + ".pdf");
                }
                
                fileContent = reportService.CreateFacultyExcel(model);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
            } catch (Exception ex) {
                TempData["Message"] = "Lỗi khi xuất file: " + ex.Message;
                return RedirectToAction("ToanKhoa", new { maKhoa = model.MaKhoa, maNamHoc = model.MaNamHoc, maHocKy = model.MaHocKy, cheDo = model.CheDo });
            }
        }

        /// <summary>
        /// Xuất dữ liệu báo cáo cấp Lớp ra file Excel hoặc PDF.
        /// </summary>
        /// <param name="model">Dữ liệu lọc báo cáo</param>
        /// <param name="format">Định dạng file xuất (excel hoặc pdf)</param>
        /// <returns>File dữ liệu kết quả</returns>
        [HttpPost]
        public ActionResult ExportClass(ClassReportViewModel model, string format)
        {
            string fileName = $"BaoCaoLop_{model.MaLop}_{DateTime.Now:yyyyMMdd}";

            try {
                CalculateClassData(model);
                byte[] fileContent;
                if (format == "pdf")
                {
                    fileContent = reportService.CreateClassPdf(model);
                    return File(fileContent, "application/pdf", fileName + ".pdf");
                }

                fileContent = reportService.CreateClassExcel(model);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
            } catch (Exception ex) {
                TempData["Message"] = "Lỗi khi xuất file: " + ex.Message;
                return RedirectToAction("ThongKeLop", new { maKhoa = model.MaKhoa, maLop = model.MaLop, maNamHoc = model.MaNamHoc, maHocKy = model.MaHocKy });
            }
        }

        /// <summary>
        /// Tính toán và tổng hợp dữ liệu báo cáo cấp Khoa dựa trên các tiêu chí lọc.
        /// </summary>
        /// <param name="model">ViewModel chứa tiêu chí lọc và là nơi lưu kết quả tính toán</param>
        private void CalculateFacultyData(FacultyReportViewModel model)
        {
            model.Events = new List<FacultyReportEventHeader>();
            model.Rows = new List<FacultyReportRow>();

            if (string.IsNullOrEmpty(model.MaKhoa)) return;

            var khoa = db.Khoas.Find(model.MaKhoa);
            model.TenKhoa = khoa?.TenKhoa;

            var eventQuery = db.SuKiens.Include(s => s.TrangThaiSuKien).Where(s => s.MaSuKienCha == null);
            
            if (!string.IsNullOrEmpty(model.MaNamHoc)) eventQuery = eventQuery.Where(s => s.MaNamHoc == model.MaNamHoc);
            if (!string.IsNullOrEmpty(model.MaHocKy)) eventQuery = eventQuery.Where(s => s.MaHocKy == model.MaHocKy);
            
            // Lấy sự kiện mà người tạo thuộc khoa này
            var events = eventQuery
                .Where(s => s.NguoiDung.LopHoc.MaKhoa == model.MaKhoa || s.NguoiDung.MaQuyen == "Q01")
                .OrderBy(s => s.ThoiGianBatDau)
                .ToList();

            model.Events = events.Select(e => new FacultyReportEventHeader
            {
                MaSuKien = e.MaSuKien,
                TenSuKien = e.TenSuKien,
                ThoiGianText = e.ThoiGianBatDau.ToString("dd/MM/yyyy"),
                LaSuKienCha = db.SuKiens.Any(x => x.MaSuKienCha == e.MaSuKien),
                SoSuKienCon = db.SuKiens.Count(x => x.MaSuKienCha == e.MaSuKien)
            }).ToList();

            var lops = db.LopHocs
                .Where(l => l.MaKhoa == model.MaKhoa)
                .OrderBy(l => l.KhoaHoc).ThenBy(l => l.TenLop)
                .ToList();

            int totalAttendances = 0;

            foreach (var lop in lops)
            {
                var row = new FacultyReportRow
                {
                    MaLop = lop.MaLop,
                    TenLop = lop.TenLop,
                    KhoaHoc = lop.KhoaHoc,
                    SiSo = db.NguoiDungs.Count(u => u.MaLop == lop.MaLop && (u.MaQuyen == "Q02" || u.MaQuyen == "Q04" || u.MaQuyen == "Q07" || u.MaQuyen == "Q08" || u.MaQuyen == "Q09" || u.MaQuyen == "Q10")),
                    Cells = new List<FacultyReportCell>()
                };

                foreach (var ev in model.Events)
                {
                    var eventIds = new List<string> { ev.MaSuKien };
                    eventIds.AddRange(db.SuKiens.Where(x => x.MaSuKienCha == ev.MaSuKien).Select(x => x.MaSuKien));

                    int participated = db.DangKy_DiemDanh
                        .Count(dk => eventIds.Contains(dk.MaSuKien) && dk.NguoiDung.MaLop == lop.MaLop && dk.ThoiGianDiemDanh != null);

                    totalAttendances += participated;
                    row.Cells.Add(new FacultyReportCell
                    {
                        MaSuKien = ev.MaSuKien,
                        SoLuongThamGia = participated,
                        SiSo = row.SiSo,
                        TyLeThamGia = row.SiSo > 0 ? (decimal)participated * 100 / row.SiSo : 0
                    });
                }
                model.Rows.Add(row);
            }

            model.TotalStudents = db.NguoiDungs.Count(u => u.LopHoc.MaKhoa == model.MaKhoa && (u.MaQuyen == "Q02" || u.MaQuyen == "Q04" || u.MaQuyen == "Q07" || u.MaQuyen == "Q08" || u.MaQuyen == "Q09" || u.MaQuyen == "Q10"));
            model.TotalAttendances = totalAttendances;
        }

        /// <summary>
        /// Tính toán và tổng hợp dữ liệu báo cáo cấp Lớp dựa trên các tiêu chí lọc.
        /// </summary>
        /// <param name="model">ViewModel chứa tiêu chí lọc và là nơi lưu kết quả tính toán</param>
        private void CalculateClassData(ClassReportViewModel model)
        {
            model.Events = new List<ClassReportEventHeader>();
            model.Rows = new List<ClassReportRow>();

            if (string.IsNullOrEmpty(model.MaLop)) return;

            var lop = db.LopHocs.Include(l => l.Khoa).FirstOrDefault(l => l.MaLop == model.MaLop);
            model.TenLop = lop?.TenLop;
            model.TenKhoa = lop?.Khoa?.TenKhoa;

            var eventQuery = db.SuKiens.Where(s => s.MaSuKienCha == null);
            if (!string.IsNullOrEmpty(model.MaNamHoc)) eventQuery = eventQuery.Where(s => s.MaNamHoc == model.MaNamHoc);
            if (!string.IsNullOrEmpty(model.MaHocKy)) eventQuery = eventQuery.Where(s => s.MaHocKy == model.MaHocKy);

            var events = eventQuery
                .Where(s => (s.CapToChuc == "Khoa" && s.NguoiDung.LopHoc.MaKhoa == model.MaKhoa) || (s.CapToChuc == "Lớp" && s.NguoiDung.MaLop == model.MaLop))
                .OrderBy(s => s.ThoiGianBatDau)
                .ToList();

            model.Events = events.Select(e => new ClassReportEventHeader
            {
                MaSuKien = e.MaSuKien,
                TenSuKien = e.TenSuKien,
                ThoiGianText = e.ThoiGianBatDau.ToString("dd/MM/yyyy"),
                LaSuKienCha = db.SuKiens.Any(x => x.MaSuKienCha == e.MaSuKien)
            }).ToList();

            var students = db.NguoiDungs
                .Where(u => u.MaLop == model.MaLop && (u.MaQuyen == "Q02" || u.MaQuyen == "Q04" || u.MaQuyen == "Q07" || u.MaQuyen == "Q08" || u.MaQuyen == "Q09" || u.MaQuyen == "Q10"))
                .OrderBy(u => u.MaSinhVien)
                .ToList();

            foreach (var sv in students)
            {
                var row = new ClassReportRow
                {
                    MaNguoiDung = sv.MaNguoiDung,
                    MaSinhVien = sv.MaSinhVien,
                    HoTen = sv.HoTen,
                    Cells = new List<ClassReportCell>()
                };

                foreach (var ev in model.Events)
                {
                    var eventIds = new List<string> { ev.MaSuKien };
                    eventIds.AddRange(db.SuKiens.Where(x => x.MaSuKienCha == ev.MaSuKien).Select(x => x.MaSuKien));

                    var registration = db.DangKy_DiemDanh
                        .Include(x => x.SuKien.MucDiemRenLuyen)
                        .Include(x => x.TrangThaiDiemDanh)
                        .FirstOrDefault(dk => eventIds.Contains(dk.MaSuKien) && dk.MaNguoiDung == sv.MaNguoiDung);

                    row.Cells.Add(new ClassReportCell
                    {
                        MaSuKien = ev.MaSuKien,
                        CoMat = registration?.ThoiGianDiemDanh != null,
                        Diem = registration?.ThoiGianDiemDanh != null ? (registration.SuKien?.MucDiemRenLuyen?.Diem ?? 0) : 0,
                        TrangThaiText = registration?.TrangThaiDiemDanh?.TenTTDD ?? "Vắng"
                    });
                }
                model.Rows.Add(row);
            }
        }

        /// <summary>
        /// Nạp các danh sách lọc (Khoa, Năm học, Học kỳ) cho báo cáo Khoa.
        /// </summary>
        private void LoadFilters(FacultyReportViewModel model, string role, string facultyId)
        {
            var khoas = role == RoleKeys.Admin ? db.Khoas.ToList() : db.Khoas.Where(k => k.MaKhoa == facultyId).ToList();
            model.KhoaList = new SelectList(khoas, "MaKhoa", "TenKhoa", model.MaKhoa);
            model.NamHocList = new SelectList(db.NamHocs.OrderByDescending(n => n.MaNamHoc).ToList(), "MaNamHoc", "TenNamHoc", model.MaNamHoc);
            model.HocKyList = new SelectList(db.HocKies.ToList(), "MaHocKy", "TenHocKy", model.MaHocKy);
        }

        /// <summary>
        /// Nạp các danh sách lọc (Khoa, Lớp, Năm học, Học kỳ) cho báo cáo Lớp.
        /// </summary>
        private void LoadClassFilters(ClassReportViewModel model, string role, string facultyId, string classId)
        {
            var khoas = (role == RoleKeys.Admin) ? db.Khoas.ToList() : db.Khoas.Where(k => k.MaKhoa == facultyId).ToList();
            model.KhoaList = new SelectList(khoas, "MaKhoa", "TenKhoa", model.MaKhoa);

            var lops = string.IsNullOrEmpty(model.MaKhoa) ? new List<LopHoc>() : db.LopHocs.Where(l => l.MaKhoa == model.MaKhoa).ToList();
            if (role == RoleKeys.ClassOfficer) lops = lops.Where(l => l.MaLop == classId).ToList();
            model.LopList = new SelectList(lops, "MaLop", "TenLop", model.MaLop);

            model.NamHocList = new SelectList(db.NamHocs.OrderByDescending(n => n.MaNamHoc).ToList(), "MaNamHoc", "TenNamHoc", model.MaNamHoc);
            model.HocKyList = new SelectList(db.HocKies.ToList(), "MaHocKy", "TenHocKy", model.MaHocKy);
        }

        /// <summary>
        /// Lấy vai trò hiện tại của người dùng.
        /// </summary>
        private string CurrentRole() => (Session["RoleKey"] ?? "").ToString();

        /// <summary>
        /// Lấy mã người dùng hiện tại.
        /// </summary>
        private string CurrentUserId() => (Session["MaNguoiDung"] ?? User.Identity.Name ?? "").ToString();

        /// <summary>
        /// Lấy mã khoa của người dùng hiện tại.
        /// </summary>
        private string CurrentFacultyId() => GetUserFacultyId(CurrentUserId());

        /// <summary>
        /// Lấy mã lớp của người dùng hiện tại.
        /// </summary>
        private string CurrentClassId() => GetUserClassId(CurrentUserId());

        /// <summary>
        /// Truy vấn mã khoa của một người dùng bất kỳ.
        /// </summary>
        private string GetUserFacultyId(string userId)
        {
            var user = db.NguoiDungs.Include(x => x.LopHoc).FirstOrDefault(x => x.MaNguoiDung == userId);
            return user?.LopHoc?.MaKhoa;
        }

        /// <summary>
        /// Truy vấn mã lớp của một người dùng bất kỳ.
        /// </summary>
        private string GetUserClassId(string userId)
        {
            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId);
            return user?.MaLop;
        }

        /// <summary>
        /// Giải phóng các tài nguyên kết nối cơ sở dữ liệu.
        /// </summary>
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}

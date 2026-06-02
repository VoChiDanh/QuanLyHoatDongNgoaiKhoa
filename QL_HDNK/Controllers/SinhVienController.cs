using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QL_HDNK.Models;
using QL_HDNK.Filters;
using QL_HDNK.Services;
using System.Collections.Generic;

namespace QL_HDNK.Controllers
{
    /// <summary>
    /// Controller dành riêng cho các chức năng của Sinh viên và xem thông tin sự kiện từ góc nhìn người học.
    /// Bao gồm: Xem danh sách sự kiện, đăng ký, xem lịch cá nhân, điểm danh QR và xem điểm rèn luyện.
    /// </summary>
    [Authorize]
    [RoleAuthorize(RoleKeys.Admin, RoleKeys.FacultyUnion, RoleKeys.ClassOfficer, RoleKeys.Student)]
    public class SinhVienController : Controller
    {
        private const int MaxEvidenceImageBytes = int.MaxValue;
        private readonly QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các sự kiện dành cho sinh viên với các bộ lọc tìm kiếm.
        /// </summary>
        /// <param name="tuKhoa">Từ khóa tìm kiếm (tên sự kiện, địa điểm, cấp tổ chức)</param>
        /// <param name="loc">Bộ lọc nhanh (sap-dien-ra, da-ket-thuc, tat-ca)</param>
        /// <param name="tuNgay">Lọc từ ngày</param>
        /// <param name="denNgay">Lọc đến ngày</param>
        /// <returns>View danh sách sự kiện cho sinh viên</returns>
        public ActionResult SuKien(string tuKhoa, string loc = "tat-ca", DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            var now = DateTime.Now;
            var hocKyHienTai = db.HocKies.FirstOrDefault(hk => hk.TuNgay <= now && hk.DenNgay >= now);
            if (hocKyHienTai != null)
            {
                ViewBag.TenHocKyHienTai = hocKyHienTai.TenHocKy;

                // Mặc định lấy thời gian của học kỳ hiện tại nếu sinh viên chưa nhập filter ngày
                if (!tuNgay.HasValue) tuNgay = hocKyHienTai.TuNgay;
                if (!denNgay.HasValue) denNgay = hocKyHienTai.DenNgay;
            }
            var query = db.SuKiens
                .Include(s => s.HocKy)
                .Include(s => s.MucDiemRenLuyen)
                .Include(s => s.NamHoc)
                .Include(s => s.NguoiDung)
                .Include(s => s.TrangThaiSuKien)
                .AsQueryable();

            // Lọc cơ bản: Chỉ hiện sự kiện đã duyệt hoặc đang mở, tránh bản nháp
            query = query.Where(s => !s.TrangThaiSuKien.TenTTSK.Contains("nháp") && !s.TrangThaiSuKien.TenTTSK.Contains("Hủy"));

            // Phân quyền hiển thị sự kiện dựa trên vai trò người dùng
            var role = CurrentRole();
            if (role == RoleKeys.FacultyUnion)
            {
                var facultyId = CurrentFacultyId();
                query = string.IsNullOrWhiteSpace(facultyId)
                    ? query.Where(s => false)
                    : query.Where(s => s.NguoiDung.LopHoc.MaKhoa == facultyId);
            }
            else if (role == RoleKeys.ClassOfficer)
            {
                var facultyId = CurrentFacultyId();
                var classId = CurrentClassId();
                query = string.IsNullOrWhiteSpace(facultyId) || string.IsNullOrWhiteSpace(classId)
                    ? query.Where(s => false)
                    : query.Where(s =>
                        (s.CapToChuc != "Lớp" && s.NguoiDung.LopHoc.MaKhoa == facultyId) ||
                        (s.CapToChuc == "Lớp" && s.NguoiDung.MaLop == classId));
            }
            else if (role == RoleKeys.Student)
            {
                var classId = CurrentClassId();
                query = string.IsNullOrWhiteSpace(classId)
                    ? query.Where(s => false)
                    : query.Where(s => s.CapToChuc == "Lớp" && s.NguoiDung.MaLop == classId);
            }

            // Lọc theo trạng thái thời gian (Sắp diễn ra / Đã kết thúc)
            if (loc == "sap-dien-ra")
            {
                query = query.Where(s => !s.TrangThaiSuKien.TenTTSK.ToLower().Contains("kết thúc"));
            }
            else if (loc == "da-ket-thuc")
            {
                query = query.Where(s => s.TrangThaiSuKien.TenTTSK.ToLower().Contains("kết thúc"));
            }

            // Lọc theo khoảng thời gian tùy chọn
            if (tuNgay.HasValue)
            {
                query = query.Where(s => s.ThoiGianBatDau >= tuNgay.Value);
            }
            if (denNgay.HasValue)
            {
                var endOfDay = denNgay.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.ThoiGianBatDau <= endOfDay);
            }

            // Tìm kiếm theo từ khóa văn bản
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(s =>
                    s.TenSuKien.Contains(tuKhoa) ||
                    s.DiaDiem.Contains(tuKhoa) ||
                    s.CapToChuc.Contains(tuKhoa));
            }

            var userId = CurrentUserId();
            var registrations = db.DangKy_DiemDanh
                .Include(x => x.SuKien)
                .Where(x => x.MaNguoiDung == userId)
                .ToList();

            var eventList = query.OrderByDescending(s => s.ThoiGianBatDau).ToList();
            var overlappingWarnings = new Dictionary<string, string>();

            foreach (var suKien in eventList)
            {
                var overlap = registrations.Select(r => r.SuKien)
                    .Where(e => e != null && e.TrangThaiSuKien != null && !e.TrangThaiSuKien.TenTTSK.Contains("Hủy"))
                    .FirstOrDefault(e => suKien.ThoiGianBatDau < e.ThoiGianKetThuc && suKien.ThoiGianKetThuc > e.ThoiGianBatDau && e.MaSuKien != suKien.MaSuKien);
                
                if (overlap != null)
                {
                    overlappingWarnings[suKien.MaSuKien] = $"Sự kiện '{overlap.TenSuKien}'";
                }
            }

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.Loc = loc;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
            ViewBag.RegisteredEventIds = registrations.Select(x => x.MaSuKien).ToList();
            ViewBag.AttendedEventIds = registrations.Where(x => x.ThoiGianDiemDanh != null || x.MaTTDD == FindAttendanceStatus(true)).Select(x => x.MaSuKien).ToList();
            ViewBag.OverlappingWarnings = overlappingWarnings;

            return View(eventList);
        }

        /// <summary>
        /// Xử lý đăng ký tham gia một sự kiện.
        /// </summary>
        /// <param name="id">Mã sự kiện muốn đăng ký</param>
        /// <returns>Redirect về trang Lịch cá nhân</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(string id)
        {
            var userId = CurrentUserId();
            var suKien = db.SuKiens.Find(id);

            if (suKien == null || string.IsNullOrEmpty(userId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Kiểm tra quyền truy cập sự kiện (cấp khoa/cấp lớp)
            if (!CanAccessEvent(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            // Kiểm tra trạng thái mở đăng ký và giới hạn số lượng
            if (!IsRegistrationOpen(suKien))
            {
                TempData["Message"] = "Sự kiện chưa mở đăng ký.";
                return RedirectToAction("SuKien");
            }

            if (IsFull(suKien))
            {
                TempData["Message"] = "Sự kiện đã đủ số lượng.";
                return RedirectToAction("SuKien");
            }

            var existed = db.DangKy_DiemDanh.Any(x => x.MaSuKien == id && x.MaNguoiDung == userId);
            if (!existed)
            {
                var dangKy = new DangKy_DiemDanh
                {
                    MaDangKy = CreateRegistrationId(),
                    MaSuKien = id,
                    MaNguoiDung = userId,
                    ThoiGianDangKy = DateTime.Now,
                    MaTTDK = FindRegistrationStatus(),
                    MaTTDD = FindAttendanceStatus(false)
                };

                db.DangKy_DiemDanh.Add(dangKy);
                suKien.SoLuongDaDangKy = suKien.SoLuongDaDangKy + 1;
                db.SaveChanges();
                TempData["Message"] = "Đăng ký thành công!";
            }

            return RedirectToAction("LichCaNhan");
        }

        /// <summary>
        /// Xử lý hủy đăng ký tham gia sự kiện (chỉ khi chưa điểm danh và sự kiện còn mở đăng ký).
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>Redirect về trang Lịch cá nhân</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyDangKy(string id)
        {
            var userId = CurrentUserId();
            var dangKy = db.DangKy_DiemDanh
                .Include(d => d.SuKien)
                .Include(d => d.SuKien.TrangThaiSuKien)
                .FirstOrDefault(x => x.MaDangKy == id && x.MaNguoiDung == userId);

            if (dangKy != null)
            {
                if (dangKy.ThoiGianDiemDanh.HasValue)
                {
                    TempData["Message"] = "Không thể hủy đăng ký khi đã điểm danh thành công.";
                    return RedirectToAction("LichCaNhan");
                }

                if (dangKy.SuKien == null || !IsRegistrationOpen(dangKy.SuKien))
                {
                    TempData["Message"] = "Chỉ có thể hủy đăng ký khi sự kiện còn ở trạng thái mở đăng ký.";
                    return RedirectToAction("LichCaNhan");
                }

                var suKien = dangKy.SuKien;

                var dangKyId = dangKy.MaDangKy;
                var thongBaoList = db.ThongBaos.Where(t => t.MaLienQuan == dangKyId).ToList();
                if (thongBaoList.Any())
                {
                    db.ThongBaos.RemoveRange(thongBaoList);
                }

                db.DangKy_DiemDanh.Remove(dangKy);
                if (suKien != null && suKien.SoLuongDaDangKy > 0)
                {
                    suKien.SoLuongDaDangKy -= 1;
                }
                db.SaveChanges();
                TempData["Message"] = "Đã hủy đăng ký sự kiện thành công.";
            }

            return RedirectToAction("LichCaNhan");
        }

        /// <summary>
        /// Xử lý điểm danh nhanh từ danh sách cá nhân.
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>Redirect về trang Lịch cá nhân</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DiemDanh(string id)
        {
            var userId = CurrentUserId();
            var dangKy = db.DangKy_DiemDanh.FirstOrDefault(x => x.MaDangKy == id && x.MaNguoiDung == userId);

            if (dangKy != null)
            {
                dangKy.ThoiGianDiemDanh = DateTime.Now;
                dangKy.MaTTDD = FindAttendanceStatus(true);
                db.SaveChanges();
            }

            return RedirectToAction("LichCaNhan");
        }

        ///// <summary>
        ///// Thực hiện đồng thời Đăng ký và Điểm danh tham gia sự kiện.
        ///// </summary>
        ///// <param name="id">Mã sự kiện</param>
        ///// <returns>Redirect về trang Lịch cá nhân</returns>
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult DiemDanhTheoSuKien(string id)
        //{
        //    var userId = CurrentUserId();
        //    var suKien = db.SuKiens.Find(id);

        //    if (suKien == null || string.IsNullOrEmpty(userId))
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    if (!CanAccessEvent(suKien))
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
        //    }

        //    if (!IsRegistrationOpen(suKien) || IsFull(suKien))
        //    {
        //        TempData["Message"] = "Sự kiện chưa mở đăng ký hoặc đã đủ số lượng.";
        //        return RedirectToAction("SuKien");
        //    }

        //    var dangKy = db.DangKy_DiemDanh.FirstOrDefault(x => x.MaSuKien == id && x.MaNguoiDung == userId);
        //    if (dangKy == null)
        //    {
        //        dangKy = new DangKy_DiemDanh
        //        {
        //            MaDangKy = CreateRegistrationId(),
        //            MaSuKien = id,
        //            MaNguoiDung = userId,
        //            ThoiGianDangKy = DateTime.Now,
        //            MaTTDK = FindRegistrationStatus()
        //        };

        //        db.DangKy_DiemDanh.Add(dangKy);
        //        suKien.SoLuongDaDangKy = suKien.SoLuongDaDangKy + 1;
        //    }

        //    dangKy.ThoiGianDiemDanh = DateTime.Now;
        //    dangKy.MaTTDD = FindAttendanceStatus(true);
        //    db.SaveChanges();

        //    return RedirectToAction("LichCaNhan");
        //}

        /// <summary>
        /// Hiển thị lịch sử đăng ký và điểm danh cá nhân của sinh viên.
        /// </summary>
        /// <returns>View lịch cá nhân</returns>
        public ActionResult LichCaNhan(string tuKhoa, string maHocKy = "", string maNamHoc = "")
        {
            // Đặt học kỳ và năm học ở thời gian hiên tại
            var now = DateTime.Now;
            if (Request.QueryString["maHocKy"] == null && Request.QueryString["maNamHoc"] == null)
            {
                var currentHocKy = db.HocKies.FirstOrDefault(x => x.TuNgay <= now && x.DenNgay >= now);
                var currentNamHoc = db.NamHocs.FirstOrDefault(x => x.TuNgay <= now && x.DenNgay >= now);

                maHocKy = currentHocKy != null ? currentHocKy.MaHocKy : "";
                maNamHoc = currentNamHoc != null ? currentNamHoc.MaNamHoc : "";
            }
            //Lấy id của tài khoản hiện tại
            var userId = CurrentUserId();
            var query = db.DangKy_DiemDanh
                .Include(x => x.SuKien)
                .Include(x => x.SuKien.HocKy)
                .Include(x => x.SuKien.NamHoc)
                .Include(x => x.SuKien.MucDiemRenLuyen)
                .Include(x => x.SuKien.TrangThaiSuKien)
                .Include(x => x.TrangThaiDangKy)
                .Include(x => x.TrangThaiDiemDanh)
                .Where(x => x.MaNguoiDung == userId)
                .AsQueryable();

            // 1. Lọc theo tên sự kiện
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(x => x.SuKien != null && x.SuKien.TenSuKien.Contains(tuKhoa));
            }

            // 2. Lọc theo Học kỳ
            if (!string.IsNullOrWhiteSpace(maHocKy))
            {
                query = query.Where(x => x.SuKien != null && x.SuKien.MaHocKy == maHocKy);
            }

            // 3. Lọc theo Năm học
            if (!string.IsNullOrWhiteSpace(maNamHoc))
            {
                query = query.Where(x => x.SuKien != null && x.SuKien.MaNamHoc == maNamHoc);
            }
          
            var items = query.OrderByDescending(x => x.ThoiGianDangKy).ToList();

            // Lưu lại từ khóa để giữ trạng thái trên View
            ViewBag.TuKhoa = tuKhoa;

            // Chuẩn bị danh sách cho Dropdown List trong view
            ViewBag.MaHocKy = new SelectList(db.HocKies.ToList(), "MaHocKy", "TenHocKy", maHocKy);
            ViewBag.MaNamHoc = new SelectList(db.NamHocs.ToList(), "MaNamHoc", "TenNamHoc", maNamHoc);

            return View(items);
        }

        /// <summary>
        /// Hiển thị thống kê điểm rèn luyện đã đạt được từ các hoạt động ngoại khóa.
        /// </summary>
        /// <returns>View điểm rèn luyện</returns>
        private DiemRenLuyenViewModel GetDiemRenLuyenData(string userId)
        {
            var now = DateTime.Now;

            // Tất cả đăng ký của user (bao gồm cả chưa điểm danh, đã điểm danh)
            var userRegistrations = db.DangKy_DiemDanh
                .Include(x => x.SuKien)
                .Include(x => x.SuKien.MucDiemRenLuyen)
                .Include(x => x.SuKien.MucDiemRenLuyen.DanhMucDiem)
                .Include(x => x.SuKien.TrangThaiSuKien)
                .Where(x => x.MaNguoiDung == userId)
                .ToList();

            // Tất cả sự kiện đã kết thúc trong hệ thống để đối chiếu mục bắt buộc
            var endedEvents = db.SuKiens
                .Include(x => x.MucDiemRenLuyen)
                .Include(x => x.MucDiemRenLuyen.DanhMucDiem)
                .Include(x => x.NguoiDung)
                .Include(x => x.NguoiDung.LopHoc)
                .Include(x => x.TrangThaiSuKien)
                .Where(x => x.TrangThaiSuKien != null &&
                    (x.ThoiGianKetThuc < now || x.TrangThaiSuKien.TenTTSK.Contains("Kết thúc")) &&
                    !x.TrangThaiSuKien.TenTTSK.Contains("Hủy"))
                .ToList()
                .Where(CanAccessEvent)
                .ToList();

            var viewModel = new DiemRenLuyenViewModel();
            var danhMucs = db.DanhMucDiems.ToList();

            foreach (var danhMuc in danhMucs)
            {
                // Kiểm tra xem danh mục có phải là mục bắt buộc (như điểm danh đầy đủ các buổi sinh hoạt, chào cờ...)
                bool isMandatory = danhMuc.TenDanhMuc.ToLower().Contains("chào cờ") ||
                                   danhMuc.TenDanhMuc.ToLower().Contains("sinh hoạt") ||
                                   danhMuc.TenDanhMuc.ToLower().Contains("đầy đủ") ||
                                   danhMuc.TenDanhMuc.ToLower().Contains("chính trị");

                var danhMucVM = new DiemRenLuyenDanhMuc
                {
                    TenDanhMuc = danhMuc.TenDanhMuc,
                    DiemToiDa = danhMuc.DiemToiDa
                };

                int tongDiemDanhMuc = isMandatory ? danhMuc.DiemToiDa : 0;
                bool hasData = false;

                if (isMandatory)
                {
                    // Xét tất cả sự kiện thuộc danh mục này ĐÃ KẾT THÚC
                    var mandatoryEvents = endedEvents.Where(e => e.MucDiemRenLuyen?.MaDanhMuc == danhMuc.MaDanhMuc).ToList();

                    // Thêm cả các sự kiện chưa kết thúc nhưng user CÓ ĐĂNG KÝ để hiển thị lên bảng
                    var registeredUpcoming = userRegistrations
                        .Select(x => x.SuKien)
                        .Where(e => e.MucDiemRenLuyen?.MaDanhMuc == danhMuc.MaDanhMuc && !IsEventEndedForScore(e, now))
                        .ToList();

                    var allRelevantEvents = mandatoryEvents.Concat(registeredUpcoming)
                        .GroupBy(e => e.MaSuKien).Select(g => g.First()) // Loại bỏ trùng lặp
                        .OrderBy(e => e.ThoiGianBatDau)
                        .ToList();

                    foreach (var ev in allRelevantEvents)
                    {
                        hasData = true;
                        var reg = userRegistrations.FirstOrDefault(x => x.MaSuKien == ev.MaSuKien);
                        bool isAttended = reg != null && (reg.ThoiGianDiemDanh != null || reg.MaTTDD == FindAttendanceStatus(true));
                        bool isEnded = IsEventEndedForScore(ev, now);

                        int diemTinh = 0;
                        if (isEnded)
                        {
                            if (!isAttended)
                            {
                                // Bất kể có đăng ký hay không, vắng mặt sự kiện bắt buộc đã kết thúc là bị trừ điểm (mặc định trừ 2)
                                diemTinh = -2;
                                tongDiemDanhMuc += diemTinh;
                            }
                        }

                        danhMucVM.ChiTiets.Add(new DiemRenLuyenChiTiet
                        {
                            TenSuKien = ev.TenSuKien,
                            TenMucDiem = ev.MucDiemRenLuyen?.TenMucDRL,
                            ThoiGianDiemDanh = reg?.ThoiGianDiemDanh,
                            DaThamGia = isAttended,
                            IsEnded = isEnded,
                            DiemCongTrangThai = diemTinh
                        });
                    }

                    // Nếu là mục bắt buộc thì cho hiện lên luôn để sinh viên thấy mình đang có điểm tối đa
                    hasData = true;
                }
                else
                {
                    // Mục thông thường: chỉ xét các sự kiện CÓ TRONG DANH SÁCH ĐĂNG KÝ của sinh viên
                    var userEventsInCat = userRegistrations.Where(x => x.SuKien?.MucDiemRenLuyen?.MaDanhMuc == danhMuc.MaDanhMuc).ToList();

                    foreach (var reg in userEventsInCat)
                    {
                        hasData = true;
                        var ev = reg.SuKien;
                        bool isAttended = reg.ThoiGianDiemDanh != null || reg.MaTTDD == FindAttendanceStatus(true);
                        bool isEnded = IsEventEndedForScore(ev, now);

                        int diemTinh = 0;
                        if (isAttended)
                        {
                            // Điểm danh thành công thì được cộng điểm
                            diemTinh = ev.MucDiemRenLuyen?.Diem ?? 0;
                            tongDiemDanhMuc += diemTinh;
                        }
                        else
                        {
                            // Không tham gia hoặc đăng ký không điểm danh thì không sao (0 điểm)
                            diemTinh = 0;
                        }

                        danhMucVM.ChiTiets.Add(new DiemRenLuyenChiTiet
                        {
                            TenSuKien = ev.TenSuKien,
                            TenMucDiem = ev.MucDiemRenLuyen?.TenMucDRL,
                            ThoiGianDiemDanh = reg.ThoiGianDiemDanh,
                            DaThamGia = isAttended,
                            DiemCongTrangThai = diemTinh
                        });
                    }
                }

                // Nếu có tương tác hoặc là danh mục bắt buộc thì mới lưu vào View
                if (hasData)
                {
                    // Chốt điểm danh mục: không được quá điểm tối đa và không được nhỏ hơn 0
                    if (tongDiemDanhMuc > danhMuc.DiemToiDa)
                    {
                        tongDiemDanhMuc = danhMuc.DiemToiDa;
                    }
                    else if (tongDiemDanhMuc < 0)
                    {
                        tongDiemDanhMuc = 0;
                    }

                    danhMucVM.DiemDatDuoc = tongDiemDanhMuc;
                    viewModel.DanhMucs.Add(danhMucVM);
                    viewModel.TongDiem += tongDiemDanhMuc;
                }
            }

            return viewModel;
        }
        public ActionResult DiemRenLuyen()
        {
            var viewModel = GetDiemRenLuyenData(CurrentUserId());

            return View(viewModel);
        }

        /// <summary>
        /// Hiển thị giao diện máy quét QR.
        /// </summary>
        public ActionResult QuetQr()
        {
            return View();
        }

        /// <summary>
        /// Trang đích sau khi quét QR sự kiện, yêu cầu sinh viên cung cấp minh chứng (ảnh).
        /// </summary>
        /// <param name="id">Mã sự kiện từ QR</param>
        /// <param name="token">Token bảo mật kiểm tra tính hợp lệ của mã QR</param>
        /// <returns>View nộp minh chứng điểm danh</returns>
        public ActionResult DiemDanhSuKien(string id, string token)
        {
            if (!QrTokenService.ValidateQrToken(id, token, DateTime.Now))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Mã QR đã hết hạn hoặc không hợp lệ.");
            }

            var userId = CurrentUserId();
            var suKien = db.SuKiens.Find(id);
            if (suKien == null || !IsAttendanceOpen(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Sự kiện chưa mở điểm danh.");
            }

            if (!CanAccessEvent(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            ViewBag.Token = QrTokenService.CreateSubmissionToken(id, userId, DateTime.Now);
            return View(suKien);
        }

        /// <summary>
        /// Xử lý nộp ảnh minh chứng điểm danh sau khi quét QR.
        /// </summary>
        /// <param name="id">Mã sự kiện</param>
        /// <param name="token">Token xác thực nộp bài</param>
        /// <param name="minhChungAnh">File ảnh minh chứng (chụp tại chỗ)</param>
        /// <returns>Redirect về Lịch cá nhân sau khi nộp thành công</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DiemDanhSuKien(string id, string token, HttpPostedFileBase minhChungAnh)
        {
            var userId = CurrentUserId();
            if (!QrTokenService.ValidateSubmissionToken(id, userId, token, DateTime.Now))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Mã QR đã hết hạn hoặc không hợp lệ.");
            }

            var suKien = db.SuKiens.Find(id);
            if (suKien == null || string.IsNullOrEmpty(userId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!CanAccessEvent(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (!IsAttendanceOpen(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Sự kiện chưa mở điểm danh.");
            }

            // Kiểm tra tính hợp lệ của file ảnh
            if (minhChungAnh == null || minhChungAnh.ContentLength == 0)
            {
                ModelState.AddModelError("minhChungAnh", "Bạn phải chụp hoặc tải ảnh minh chứng điểm danh.");
            }
            else if (!IsImageFile(minhChungAnh))
            {
                ModelState.AddModelError("minhChungAnh", "Minh chứng phải là file ảnh.");
            }
            else if (minhChungAnh.ContentLength > MaxEvidenceImageBytes)
            {
                ModelState.AddModelError("minhChungAnh", "Ảnh minh chứng vượt quá giới hạn máy chủ cho phép.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Token = token;
                return View(suKien);
            }

            var registration = db.DangKy_DiemDanh.FirstOrDefault(x => x.MaSuKien == id && x.MaNguoiDung == userId);
            if (registration == null)
            {
                registration = new DangKy_DiemDanh
                {
                    MaDangKy = CreateRegistrationId(),
                    MaSuKien = id,
                    MaNguoiDung = userId,
                    ThoiGianDangKy = DateTime.Now,
                    MaTTDK = FindRegistrationStatus()
                };
                db.DangKy_DiemDanh.Add(registration);
                suKien.SoLuongDaDangKy = suKien.SoLuongDaDangKy + 1;
            }

            // Lưu ảnh và cập nhật trạng thái "Chờ duyệt"
            var imagePath = SaveEvidenceImage(minhChungAnh, registration.MaDangKy);
            registration.MinhChung = "QRPHOTO:" + imagePath;
            registration.MaTTDD = FindPendingAttendanceStatus();
            registration.ThoiGianDiemDanh = null;
            registration.NguoiDuyet = null;
            registration.ThoiGianDuyet = null;
            registration.GhiChu = "Sinh viên đã quét QR và gửi ảnh minh chứng lúc " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ", chờ cán bộ duyệt điểm danh.";
            db.SaveChanges();

            // Thông báo cho cán bộ có minh chứng mới cần duyệt
            new NotificationService(db).NotifyEvidenceSubmitted(registration);
            TempData["Message"] = "Đã gửi minh chứng điểm danh. Trạng thái đang chờ cán bộ duyệt.";
            return RedirectToAction("LichCaNhan");
        }

        /// <summary>
        /// Lấy mã người dùng hiện tại từ Session.
        /// </summary>
        private string CurrentUserId()
        {
            return (Session["MaNguoiDung"] ?? User.Identity.Name ?? "").ToString();
        }

        /// <summary>
        /// Lấy vai trò hiện tại của người dùng.
        /// </summary>
        private string CurrentRole()
        {
            return (Session["RoleKey"] ?? "").ToString();
        }

        /// <summary>
        /// Kiểm tra sinh viên có quyền tiếp cận sự kiện này hay không (dựa trên Khoa/Lớp).
        /// </summary>
        private bool CanAccessEvent(SuKien suKien)
        {
            var role = CurrentRole();
            if (role == RoleKeys.Admin)
            {
                return true;
            }

            if (role == RoleKeys.FacultyUnion)
            {
                return IsSameFacultyEvent(suKien);
            }

            if (role == RoleKeys.ClassOfficer)
            {
                return IsClassLevel(suKien.CapToChuc)
                    ? IsSameClassEvent(suKien)
                    : IsSameFacultyEvent(suKien);
            }

            return role == RoleKeys.Student && IsClassLevel(suKien.CapToChuc) && IsSameClassEvent(suKien);
        }

        /// <summary>
        /// Lấy mã Khoa của người dùng hiện tại.
        /// </summary>
        private string CurrentFacultyId()
        {
            return GetUserFacultyId(CurrentUserId());
        }

        /// <summary>
        /// Lấy mã Lớp của người dùng hiện tại.
        /// </summary>
        private string CurrentClassId()
        {
            var userId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return db.NguoiDungs
                .Where(x => x.MaNguoiDung == userId)
                .Select(x => x.MaLop)
                .FirstOrDefault();
        }

        /// <summary>
        /// Kiểm tra sự kiện có thuộc về Khoa của người dùng hiện tại hay không.
        /// </summary>
        private bool IsSameFacultyEvent(SuKien suKien)
        {
            var facultyId = CurrentFacultyId();
            return suKien != null &&
                !string.IsNullOrWhiteSpace(facultyId) &&
                GetUserFacultyId(suKien.MaNguoiTao) == facultyId;
        }

        /// <summary>
        /// Kiểm tra sự kiện có thuộc về Lớp của người dùng hiện tại hay không.
        /// </summary>
        private bool IsSameClassEvent(SuKien suKien)
        {
            var classId = CurrentClassId();
            return suKien != null &&
                !string.IsNullOrWhiteSpace(classId) &&
                GetUserClassId(suKien.MaNguoiTao) == classId;
        }

        /// <summary>
        /// Kiểm tra cấp tổ chức có phải là "Lớp" hay không.
        /// </summary>
        private bool IsClassLevel(string capToChuc)
        {
            return !string.IsNullOrWhiteSpace(capToChuc) &&
                (capToChuc.Contains("Lớp") || capToChuc.Equals("Lop", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Truy vấn mã Khoa của một người dùng bất kỳ.
        /// </summary>
        private string GetUserFacultyId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var user = db.NguoiDungs.Include(x => x.LopHoc).FirstOrDefault(x => x.MaNguoiDung == userId);
            return user != null && user.LopHoc != null ? user.LopHoc.MaKhoa : null;
        }

        /// <summary>
        /// Truy vấn mã Lớp của một người dùng bất kỳ.
        /// </summary>
        private string GetUserClassId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return db.NguoiDungs
                .Where(x => x.MaNguoiDung == userId)
                .Select(x => x.MaLop)
                .FirstOrDefault();
        }

        /// <summary>
        /// Tự động sinh mã đăng ký mới.
        /// </summary>
        private string CreateRegistrationId()
        {
            var next = db.DangKy_DiemDanh.Count() + 1;
            string id;

            do
            {
                id = "DK" + next.ToString("0000");
                next++;
            }
            while (db.DangKy_DiemDanh.Any(x => x.MaDangKy == id));

            return id;
        }

        /// <summary>
        /// Tìm mã trạng thái đăng ký mặc định khi mới đăng ký.
        /// </summary>
        private string FindRegistrationStatus()
        {
            var statuses = db.TrangThaiDangKies.ToList();
            // Ưu tiên "Đã đăng ký" hoặc "Thành công" hoặc "Đã xác nhận", tránh "Hết hạn" hoặc "Hủy"
            var status = statuses.FirstOrDefault(x => x.TenTTDK.Equals("Đã đăng ký", StringComparison.OrdinalIgnoreCase))
                         ?? statuses.FirstOrDefault(x => x.TenTTDK.Contains("xác nhận") || x.TenTTDK.Contains("thành công"))
                         ?? statuses.FirstOrDefault(x => (x.TenTTDK.Contains("Đăng") || x.TenTTDK.Contains("đăng")) && !x.TenTTDK.Contains("hết hạn") && !x.TenTTDK.Contains("Hủy"));

            return status != null ? status.MaTTDK : db.TrangThaiDangKies.Select(x => x.MaTTDK).FirstOrDefault();
        }

        /// <summary>
        /// Kiểm tra xem sự kiện có đang trong giai đoạn mở đăng ký hay không.
        /// </summary>
        private bool IsRegistrationOpen(SuKien suKien)
        {
            if (suKien.TrangThaiSuKien == null)
            {
                db.Entry(suKien).Reference(x => x.TrangThaiSuKien).Load();
            }

            var statusName = suKien.TrangThaiSuKien != null ? suKien.TrangThaiSuKien.TenTTSK : "";
            // Kiểm tra chính xác trạng thái "mở" nhưng không phải "đã đóng" hoặc "kết thúc"
            return (statusName.Contains("mở") || statusName.Contains("Mở")) &&
                   !statusName.Contains("Đóng") && !statusName.Contains("đóng") &&
                   !statusName.Contains("Kết thúc") && !statusName.Contains("kết thúc");
        }

        /// <summary>
        /// Kiểm tra xem sự kiện có đang trong giai đoạn cho phép điểm danh hay không (Đóng đăng ký / Đang diễn ra).
        /// </summary>
        private bool IsAttendanceOpen(SuKien suKien)
        {
            if (suKien.TrangThaiSuKien == null)
            {
                db.Entry(suKien).Reference(x => x.TrangThaiSuKien).Load();
            }

            var statusName = suKien.TrangThaiSuKien != null ? suKien.TrangThaiSuKien.TenTTSK.ToLower() : "";
            // Cho phép điểm danh khi trạng thái có chứa "mở" (mở đăng ký), "đóng" (đóng đăng ký) hoặc "diễn ra" (đang diễn ra)
            return statusName.Contains("mở") || statusName.Contains("đóng") || statusName.Contains("diễn ra");
        }

        /// <summary>
        /// Kiểm tra sự kiện đã đủ điều kiện chốt điểm rèn luyện hay chưa.
        /// </summary>
        private bool IsEventEndedForScore(SuKien suKien, DateTime now)
        {
            if (suKien == null)
            {
                return false;
            }

            if (suKien.ThoiGianKetThuc < now)
            {
                return true;
            }

            if (suKien.TrangThaiSuKien == null)
            {
                db.Entry(suKien).Reference(x => x.TrangThaiSuKien).Load();
            }

            var statusName = suKien.TrangThaiSuKien != null ? suKien.TrangThaiSuKien.TenTTSK : "";
            return statusName.Contains("Kết thúc") || statusName.Contains("kết thúc");
        }

        /// <summary>
        /// Kiểm tra sự kiện đã đủ số lượng đăng ký tối đa hay chưa.
        /// </summary>
        private bool IsFull(SuKien suKien)
        {
            return suKien.SoLuongToiDa.HasValue && suKien.SoLuongDaDangKy >= suKien.SoLuongToiDa.Value;
        }

        /// <summary>
        /// Kiểm tra xem file tải lên có phải định dạng ảnh hay không.
        /// </summary>
        private bool IsImageFile(HttpPostedFileBase file)
        {
            return file != null &&
                !string.IsNullOrWhiteSpace(file.ContentType) &&
                file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Lưu ảnh minh chứng điểm danh vào thư mục Content/MinhChungDiemDanh.
        /// </summary>
        /// <param name="file">File ảnh từ form</param>
        /// <param name="registrationId">Mã đăng ký dùng để đặt tên file</param>
        /// <returns>Đường dẫn tương đối tới file ảnh đã lưu</returns>
        private string SaveEvidenceImage(HttpPostedFileBase file, string registrationId)
        {
            var uploadRoot = Server.MapPath("~/Content/MinhChungDiemDanh");
            if (!Directory.Exists(uploadRoot))
            {
                Directory.CreateDirectory(uploadRoot);
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            var fileName = registrationId + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension.ToLowerInvariant();
            var fullPath = Path.Combine(uploadRoot, fileName);
            file.SaveAs(fullPath);

            return "/Content/MinhChungDiemDanh/" + fileName;
        }

        /// <summary>
        /// Tìm mã trạng thái điểm danh phù hợp (Có mặt / Chưa điểm danh).
        /// </summary>
        private string FindAttendanceStatus(bool attended)
        {
            var statuses = db.TrangThaiDiemDanhs.ToList();
            TrangThaiDiemDanh status = null;

            if (attended)
            {
                status = statuses.FirstOrDefault(x => x.TenTTDD.Equals("Có mặt", StringComparison.OrdinalIgnoreCase))
                         ?? statuses.FirstOrDefault(x => x.TenTTDD.Contains("Đã") || x.TenTTDD.Contains("hợp lệ"));
            }
            else
            {
                status = statuses.FirstOrDefault(x => x.TenTTDD.Equals("Chưa điểm danh", StringComparison.OrdinalIgnoreCase))
                         ?? statuses.FirstOrDefault(x => x.TenTTDD.Contains("Chưa") || x.TenTTDD.Contains("Vắng"));
            }

            return status != null ? status.MaTTDD : db.TrangThaiDiemDanhs.Select(x => x.MaTTDD).FirstOrDefault();
        }

        /// <summary>
        /// Tìm mã trạng thái "Chờ duyệt" khi sinh viên nộp minh chứng.
        /// </summary>
        private string FindPendingAttendanceStatus()
        {
            var status = db.TrangThaiDiemDanhs.FirstOrDefault(x =>
                x.TenTTDD.Contains("Chờ duyệt") ||
                x.TenTTDD.Contains("chờ duyệt") ||
                x.TenTTDD.Contains("Minh chứng"));

            return status != null ? status.MaTTDD : FindAttendanceStatus(false);
        }

        /// <summary>
        /// Giải phóng tài nguyên kết nối cơ sở dữ liệu.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Xử lý yêu cầu xuất file PDF phiếu thống kê hoạt động ngoại khóa cá nhân.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XuatPdfCaNhan()
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var student = db.NguoiDungs.Include(u => u.LopHoc).FirstOrDefault(u => u.MaNguoiDung == userId);
            if (student == null) return HttpNotFound();

            var now = DateTime.Now;
            var currentHocKy = db.HocKies.FirstOrDefault(hk => hk.TuNgay <= now && hk.DenNgay >= now);
            var currentNamHoc = db.NamHocs.FirstOrDefault(nh => nh.TuNgay <= now && nh.DenNgay >= now);

            // GỌI HÀM DÙNG CHUNG ĐỂ LẤY DATA KHỚP 100% VỚI GIAO DIỆN WEB
            var reportData = GetDiemRenLuyenData(userId);

            try
            {
                var pdfService = new PersonalReportService();
                byte[] fileContent = pdfService.CreatePersonalPdf(student, currentHocKy, currentNamHoc, reportData);

                string fileName = $"PhieuHoatDong_{student.MaSinhVien}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(fileContent, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi khi xuất file PDF: " + ex.Message;
                return RedirectToAction("DiemRenLuyen");
            }
        }
    }
}

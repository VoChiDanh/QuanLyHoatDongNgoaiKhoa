using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QL_HDNK.Filters;
using QL_HDNK.Models;
using QL_HDNK.Services;
using QRCoder;

namespace QL_HDNK.Controllers
{
    /// <summary>
    /// Controller quản lý các hoạt động liên quan đến Sự kiện (SuKien).
    /// Bao gồm: Danh sách, Chi tiết, Thêm mới, Chỉnh sửa, Duyệt, Điểm danh QR, Xuất báo cáo.
    /// </summary>
    [RoleAuthorize(RoleKeys.Admin, RoleKeys.FacultyUnion, RoleKeys.ClassOfficer)]
    public class SuKiensController : Controller
    {
        private readonly QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các sự kiện với các bộ lọc tìm kiếm.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (tên sự kiện, địa điểm)</param>
        /// <param name="maHocKy">Mã học kỳ để lọc</param>
        /// <param name="maNamHoc">Mã năm học để lọc</param>
        /// <param name="capToChuc">Cấp tổ chức (Lớp, Khoa, Trường)</param>
        /// <param name="maTTSK">Mã trạng thái sự kiện</param>
        /// <returns>View danh sách sự kiện</returns>
        public ActionResult Index(string search, string maHocKy, string maNamHoc, string capToChuc, string maTTSK)
        {
            ViewBag.CanApproveEvents = CanApproveEvents();
            ViewBag.CanDeleteEvents = CanDeleteEvents();

            var query = db.SuKiens
                .Include(s => s.HocKy)
                .Include(s => s.MucDiemRenLuyen)
                .Include(s => s.NamHoc)
                .Include(s => s.NguoiDung)
                .Include(s => s.NguoiDung.LopHoc)
                .Include(s => s.NguoiDung.LopHoc.Khoa)
                .Include(s => s.TrangThaiSuKien)
                .AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.TenSuKien.Contains(search) || s.DiaDiem.Contains(search));
            }

            // Lọc theo học kỳ
            if (!string.IsNullOrEmpty(maHocKy))
            {
                query = query.Where(s => s.MaHocKy == maHocKy);
            }

            // Lọc theo năm học
            if (!string.IsNullOrEmpty(maNamHoc))
            {
                query = query.Where(s => s.MaNamHoc == maNamHoc);
            }

            // Lọc theo cấp tổ chức
            if (!string.IsNullOrEmpty(capToChuc))
            {
                query = query.Where(s => s.CapToChuc == capToChuc);
            }

            // Lọc theo trạng thái sự kiện
            if (!string.IsNullOrEmpty(maTTSK))
            {
                query = query.Where(s => s.MaTTSK == maTTSK);
            }

            // Phân quyền hiển thị dữ liệu dựa trên vai trò
            var role = CurrentRole();
            if (role == RoleKeys.FacultyUnion)
            {
                var facultyId = CurrentFacultyId();
                query = string.IsNullOrWhiteSpace(facultyId)
                    ? query
                    : query.Where(s => s.NguoiDung.LopHoc.MaKhoa == facultyId);
            }
            else if (role == RoleKeys.ClassOfficer)
            {
                var classId = CurrentClassId();
                query = string.IsNullOrWhiteSpace(classId)
                    ? query.Where(s => false)
                    : query.Where(s => s.CapToChuc == "Lớp" && s.NguoiDung.MaLop == classId);
            }

            // Chuẩn bị dữ liệu cho các SelectList trong View
            ViewBag.MaHocKy = new SelectList(db.HocKies.OrderByDescending(x => x.TenHocKy), "MaHocKy", "TenHocKy", maHocKy);
            ViewBag.MaNamHoc = new SelectList(db.NamHocs.OrderByDescending(x => x.TenNamHoc), "MaNamHoc", "TenNamHoc", maNamHoc);
            ViewBag.MaTTSK = new SelectList(db.TrangThaiSuKiens.OrderBy(x => x.TenTTSK), "MaTTSK", "TenTTSK", maTTSK);

            var levels = new List<string> { "Lớp", "Khoa" };
            ViewBag.CapToChuc = new SelectList(levels, capToChuc);
            ViewBag.CurrentSearch = search;

            var items = query.OrderByDescending(s => s.ThoiGianBatDau).ToList();
            // Danh sách các sự kiện mà người dùng hiện tại có quyền quản lý hoặc chờ duyệt
            ViewBag.ManageableEventIds = items.Where(CanManageEvent).Select(s => s.MaSuKien).ToList();
            ViewBag.PendingApprovalEventIds = items.Where(IsPendingEventApproval).Select(s => s.MaSuKien).ToList();
            ViewBag.CancellableEventIds = items.Where(CanCancelEvent).Select(s => s.MaSuKien).ToList();
            return View(items);
        }

        /// <summary>
        /// Phê duyệt sự kiện (thường dùng cho Admin hoặc BCH Khoa duyệt sự kiện của Lớp).
        /// </summary>
        /// <param name="id">Mã sự kiện cần duyệt</param>
        /// <returns>Chuyển hướng về trang Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Duyet(string id)
        {
            if (!CanApproveEvents())
            {
                return View("Forbidden");
            }

            var suKien = db.SuKiens.Include(s => s.NguoiDung).Include(s => s.TrangThaiSuKien).FirstOrDefault(s => s.MaSuKien == id);
            if (suKien != null && !CanManageEvent(suKien))
            {
                return View("Forbidden");
            }
            if (suKien == null)
            {
                return HttpNotFound();
            }

            // Cập nhật trạng thái sang "Đang mở đăng ký"
            suKien.MaTTSK = FindEventStatus("Đang mở đăng ký");
            if (!CanManageEvent(suKien))
            {
                return View("Forbidden");
            }

            suKien.LyDoHuy = null;
            db.SaveChanges();

            // Xử lý thông báo
            var notificationService = new NotificationService(db);
            notificationService.MarkRelatedRead("EVENT_APPROVAL_NEEDED", suKien.MaSuKien);
            notificationService.NotifyEventCreator(suKien, true);
            NotifyEventCreator(suKien, true);
            TempData["Message"] = "Đã duyệt sự kiện và mở đăng ký.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Từ chối sự kiện (thường dùng cho Admin hoặc BCH Khoa từ chối sự kiện của Lớp).
        /// </summary>
        /// <param name="id">Mã sự kiện cần từ chối</param>
        /// <param name="lyDo">Lý do từ chối</param>
        /// <returns>Chuyển hướng về trang Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoi(string id, string lyDo)
        {
            if (!CanApproveEvents())
            {
                return View("Forbidden");
            }

            var suKien = db.SuKiens.Include(s => s.NguoiDung).Include(s => s.TrangThaiSuKien).FirstOrDefault(s => s.MaSuKien == id);
            if (suKien != null && !CanManageEvent(suKien))
            {
                return View("Forbidden");
            }
            if (suKien == null)
            {
                return HttpNotFound();
            }

            // Cập nhật trạng thái sang "Bị hủy bỏ"
            suKien.MaTTSK = FindEventStatus("Bị hủy bỏ");

            suKien.LyDoHuy = string.IsNullOrWhiteSpace(lyDo) ? "Không có lý do" : lyDo;
            db.SaveChanges();

            // Xử lý thông báo
            var notificationService = new NotificationService(db);
            notificationService.MarkRelatedRead("EVENT_APPROVAL_NEEDED", suKien.MaSuKien);
            notificationService.NotifyEventCreator(suKien, false);
            NotifyEventCreator(suKien, false);
            TempData["Message"] = "Đã từ chối sự kiện.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Thay đổi trạng thái của sự kiện (mở đăng ký, đóng, kết thúc, hủy...).
        /// </summary>
        /// <param name="id">Mã sự kiện</param>
        /// <param name="status">Slug của trạng thái cần chuyển sang</param>
        /// <returns>Chuyển hướng về trang Chi tiết sự kiện</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeStatus(string id, string status)
        {
            var suKien = db.SuKiens.Include(s => s.TrangThaiSuKien).FirstOrDefault(s => s.MaSuKien == id);
            if (suKien == null) return HttpNotFound();

            // Chỉ người quản lý mới được đổi trạng thái
            if (!CanManageEvent(suKien))
            {
                return View("Forbidden");
            }

            string targetStatusName = "";
            switch (status)
            {
                case "mo-dang-ky": targetStatusName = "Đang mở đăng ký"; break;
                case "dang-dien-ra": targetStatusName = "Đang diễn ra"; break;
                case "dong-dang-ky": targetStatusName = "đóng đăng ký"; break;
                case "ket-thuc": targetStatusName = "Đã kết thúc"; break;
                case "tam-hoan": targetStatusName = "Đã tạm hoãn"; break;
                case "huy-bo": targetStatusName = "Bị hủy bỏ"; break;
                default:
                    TempData["Message"] = "Trạng thái không hợp lệ.";
                    return RedirectToAction("Details", new { id = id });
            }

            var statusId = FindEventStatus(targetStatusName);
            if (string.IsNullOrEmpty(statusId))
            {
                TempData["Message"] = "Không tìm thấy mã trạng thái tương ứng trong hệ thống.";
                return RedirectToAction("Details", new { id = id });
            }

            suKien.MaTTSK = statusId;
            db.SaveChanges();

            TempData["Message"] = "Đã cập nhật trạng thái sự kiện thành: " + targetStatusName;
            return RedirectToAction("Details", new { id = id });
        }

        /// <summary>
        /// Hiển thị thông tin chi tiết của một sự kiện.
        /// </summary>
        /// <param name="id">Mã sự kiện</param>
        /// <returns>View chi tiết sự kiện</returns>
        public ActionResult Details(string id)
        {
            var suKien = db.SuKiens.Include(s => s.TrangThaiSuKien).FirstOrDefault(s => s.MaSuKien == id);
            ViewBag.DiemDanhUrl = CreateAttendanceUrl(id);
            ViewBag.CanManageEvent = CanManageEvent(suKien);
            ViewBag.CanApproveEvents = CanApproveEvents();
            ViewBag.CanDeleteEvents = CanDeleteEvents();
            ViewBag.CanCancelEvent = CanCancelEvent(suKien);
            ViewBag.IsPendingApproval = IsPendingEventApproval(suKien);
            return View(suKien);
        }

        /// <summary>
        /// Hiển thị form tạo mới sự kiện.
        /// </summary>
        /// <returns>View tạo mới</returns>
        public ActionResult Create()
        {
            var now = DateTime.Now;

            // Tìm học kỳ và năm học mặc định dựa trên ngày hiện tại
            var defaultHocKy = db.HocKies.FirstOrDefault(x => x.TuNgay <= now && x.DenNgay >= now);
            var defaultNamHoc = db.NamHocs.FirstOrDefault(x => x.TuNgay <= now && x.DenNgay >= now);

            var model = new SuKien
            {
                MaSuKien = CreateEventId(), //Hàm tạo mã sự kiện tăng tự động SKxxx
                MaNguoiTao = CurrentUserId(),
                ThoiGianBatDau = now.AddDays(3), // Mặc định là 3 ngày sau để tránh việc sự kiện vừa tạo đã bị tính là bắt đầu
                ThoiGianKetThuc = now.AddDays(3).AddHours(2),
                MaHocKy = defaultHocKy != null ? defaultHocKy.MaHocKy : null,
                MaNamHoc = defaultNamHoc != null ? defaultNamHoc.MaNamHoc : null,
                CapToChuc = CurrentRole() == RoleKeys.ClassOfficer ? "Lớp" : "Khoa", // thiết lập dựa vào role người tạo
                //Trạng thái sự kiện là Chờ kiểm duyệt nếu role là BCS lớp ;là Đang mở đăng ký nếu role là BHC đoàn khoa
                MaTTSK = CurrentRole() == RoleKeys.ClassOfficer ? FindEventStatus("Chờ kiểm duyệt") : FindEventStatus("Đang mở đăng ký"),
                SoLuongDaDangKy = 0
            };
            // load SelectList cần thiết cho View (Học kỳ, Năm học, Mức điểm...).
            LoadSelectLists(model);
            return View(model);
        }

        /// <summary>
        /// Xử lý lưu thông tin sự kiện mới vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="suKien">Dữ liệu sự kiện từ form</param>
        /// <returns>Redirect về Index nếu thành công, ngược lại hiển thị lại form lỗi</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaSuKien,TenSuKien,Noidung,MaHocKy,MaNamHoc,DiaDiem,SoLuongToiDa,MaMucDRL,CapToChuc,MaTTSK,LyDoHuy")] SuKien suKien)
        {
            ApplyEventDatesFromRequest(suKien);
            suKien.MaNguoiTao = CurrentUserId();
            suKien.SoLuongDaDangKy = 0;
            //Trạng thái sự kiện là Chờ kiểm duyệt nếu role là BCS lớp ;là Đang mở đăng ký nếu role là BHC đoàn khoa
            if (CurrentRole() == RoleKeys.ClassOfficer)
            {
                suKien.CapToChuc = "Lớp";
                suKien.MaTTSK = FindEventStatus("Chờ kiểm duyệt");
            }
            else
            {
                suKien.MaTTSK = FindEventStatus("Đang mở đăng ký");
                suKien.LyDoHuy = null;
            }
            //Kiểm tra thời gian (kết thúc < bắt đầu)
            ValidateEventTime(suKien);
            if (ModelState.IsValid)
            {
                db.SuKiens.Add(suKien);
                db.SaveChanges();
                // lưu sự kiện cha
                SaveParentEvent(suKien.MaSuKien);
                // gửi thông báo cho các bsc khác cùng lớp để nắm thông tin và gửi thông báo để bên trên duyệt sự kiện (nếu là sự kiện cấp lớp)
                NotifyApproversForPendingEvent(suKien);
                NotifyClassOfficersForFacultyEvent(suKien);
                return RedirectToAction("Index");
            }
            // load SelectList cần thiết cho View (Học kỳ, Năm học, Mức điểm...).
            LoadSelectLists(suKien);
            return View(suKien);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa thông tin sự kiện.
        /// </summary>
        /// <param name="id">Mã sự kiện cần sửa</param>
        /// <returns>View chỉnh sửa</returns>
        public ActionResult Edit(string id)
        {
            var suKien = db.SuKiens.Find(id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            LoadSelectLists(suKien);
            return View(suKien);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin sự kiện sau khi sửa.
        /// </summary>
        /// <param name="input">Dữ liệu sự kiện đã sửa</param>
        /// <returns>Redirect về Index nếu thành công</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaSuKien,TenSuKien,Noidung,MaHocKy,MaNamHoc,DiaDiem,SoLuongToiDa,MaMucDRL,CapToChuc,MaTTSK,LyDoHuy")] SuKien input)
        {
            ApplyEventDatesFromRequest(input);
            var suKien = db.SuKiens.Find(input.MaSuKien);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            if (CurrentRole() == RoleKeys.ClassOfficer)
            {
                input.CapToChuc = "Lớp";
                //đưa về trạng thái chờ kiểm duyệt 
                input.MaTTSK = FindEventStatus("Chờ kiểm duyệt");
                input.LyDoHuy = null;
            }
            else
            {
                input.MaTTSK = suKien.MaTTSK;
            }

            ValidateEventTime(input);

            if (ModelState.IsValid)
            {
                suKien.TenSuKien = input.TenSuKien;
                suKien.Noidung = input.Noidung;
                suKien.ThoiGianBatDau = input.ThoiGianBatDau;
                suKien.ThoiGianKetThuc = input.ThoiGianKetThuc;
                suKien.MaHocKy = input.MaHocKy;
                suKien.MaNamHoc = input.MaNamHoc;
                suKien.DiaDiem = input.DiaDiem;
                suKien.SoLuongToiDa = input.SoLuongToiDa;
                suKien.MaMucDRL = input.MaMucDRL;
                suKien.CapToChuc = input.CapToChuc;
                suKien.MaTTSK = input.MaTTSK;
                suKien.LyDoHuy = input.LyDoHuy;
                db.SaveChanges();
                SaveParentEvent(suKien.MaSuKien);

                if (CurrentRole() == RoleKeys.ClassOfficer)
                {
                    NotifyApproversForPendingEvent(suKien, true);
                    TempData["Message"] = "Đã gửi lại sự kiện để chờ duyệt.";
                }

                return RedirectToAction("Index");
            }

            LoadSelectLists(input);
            return View(input);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa sự kiện.
        /// </summary>
        /// <param name="id">Mã sự kiện cần xóa</param>
        /// <returns>View xác nhận xóa</returns>
        /// <summary>
        /// Hiển thị trang xác nhận hủy sự kiện.
        /// </summary>
        /// <param name="id">Mã sự kiện cần hủy</param>
        /// <returns>View xác nhận hủy</returns>
        public ActionResult Cancel(string id)
        {
            var suKien = db.SuKiens.Find(id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            if (!CanManageEvent(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (!CanCancelEvent(suKien))
            {
                TempData["Message"] = "Không thể hủy sự kiện này ở trạng thái hiện tại hoặc do giới hạn quyền hạn.";
                return RedirectToAction("Details", new { id });
            }

            return View(suKien);
        }

        /// <summary>
        /// Xử lý hủy sự kiện, bao gồm hủy sự kiện con và các đăng ký.
        /// </summary>
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public ActionResult CancelConfirmed(string id, string lyDoHuy)
        {
            var suKien = db.SuKiens.Include(s => s.SuKien1).SingleOrDefault(s => s.MaSuKien == id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            if (!CanManageEvent(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (!CanCancelEvent(suKien))
            {
                TempData["Message"] = "Không thể hủy sự kiện này ở trạng thái hiện tại hoặc do giới hạn quyền hạn.";
                return RedirectToAction("Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                ModelState.AddModelError("", "Vui lòng nhập lý do hủy sự kiện.");
                return View(suKien);
            }

            // Danh sách các sự kiện cần xử lý (sự kiện cha + con)
            var eventIdsToCancel = new List<string> { suKien.MaSuKien };
            foreach (var child in suKien.SuKien1)
            {
                eventIdsToCancel.Add(child.MaSuKien);
                child.MaTTSK = "TTSK08";
                child.LyDoHuy = lyDoHuy;
            }

            suKien.MaTTSK = "TTSK08";
            suKien.LyDoHuy = lyDoHuy;

            // Xử lý đăng ký điểm danh
            var registrations = db.DangKy_DiemDanh.Where(d => eventIdsToCancel.Contains(d.MaSuKien)).ToList();
            var notificationService = new NotificationService(db);

            foreach (var reg in registrations)
            {
                // Cập nhật trạng thái đăng ký thành "Đã hủy bởi hệ thống" (TTDK10)
                reg.MaTTDK = "TTDK10";

                // Gửi thông báo cho sinh viên
                notificationService.Notify(
                    reg.MaNguoiDung,
                    "Sự kiện bị hủy",
                    "Sự kiện \"" + suKien.TenSuKien + "\" đã bị hủy. Lý do: " + lyDoHuy,
                    "/SinhVien/LichCaNhan",
                    "EVENT_CANCELLED",
                    suKien.MaSuKien
                );
            }

            db.SaveChanges();
            TempData["Message"] = "Đã hủy sự kiện thành công.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa sự kiện (Xóa cứng).
        /// </summary>
        /// <param name="id">Mã sự kiện cần xóa</param>
        /// <returns>View xác nhận xóa</returns>
        public ActionResult Delete(string id)
        {
            var suKien = db.SuKiens.Find(id);
            if (suKien == null)
            {
                return HttpNotFound();
            }
            if (!CanDeleteEvents())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            return View(suKien);
        }

        /// <summary>
        /// Xử lý xóa sự kiện khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã sự kiện cần xóa</param>
        /// <returns>Redirect về Index</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            if (!CanDeleteEvents())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            var suKien = db.SuKiens.Include(s => s.SuKien1).SingleOrDefault(s => s.MaSuKien == id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            var eventIdsToDelete = new List<string> { id };
            eventIdsToDelete.AddRange(suKien.SuKien1.Select(s => s.MaSuKien));

            // Xóa Đăng ký điểm danh
            var registrations = db.DangKy_DiemDanh.Where(d => eventIdsToDelete.Contains(d.MaSuKien)).ToList();
            db.DangKy_DiemDanh.RemoveRange(registrations);

            // Xóa Thông báo liên quan (nếu MaLienQuan khớp)
            var notifications = db.ThongBaos.Where(t => eventIdsToDelete.Contains(t.MaLienQuan)).ToList();
            db.ThongBaos.RemoveRange(notifications);

            // Xóa sự kiện con
            var childEvents = db.SuKiens.Where(s => s.MaSuKienCha == id).ToList();
            db.SuKiens.RemoveRange(childEvents);

            // Xóa sự kiện cha
            db.SuKiens.Remove(suKien);

            db.SaveChanges();
            TempData["Message"] = "Đã xóa sự kiện thành công.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Tạo và trả về ảnh mã QR dùng để điểm danh cho sự kiện.
        /// </summary>
        /// <param name="id">Mã sự kiện</param>
        /// <returns>File ảnh PNG chứa mã QR</returns>
        public ActionResult QrDiemDanh(string id)
        {
            var suKien = db.SuKiens.Find(id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            if (!CanManageEvent(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (!IsAttendanceOpen(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Sự kiện chưa được duyệt/chưa mở điểm danh.");
            }

            var url = CreateAttendanceUrl(id);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddSeconds(-1));
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
            using (var qr = new QRCode(data))
            using (var bitmap = qr.GetGraphic(18))
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return File(stream.ToArray(), "image/png");
            }
        }

        /// <summary>
        /// Trả về URL điểm danh dưới dạng JSON.
        /// </summary>
        /// <param name="id">Mã sự kiện</param>
        /// <returns>JSON chứa URL điểm danh</returns>
        public ActionResult DiemDanhUrl(string id)
        {
            var suKien = db.SuKiens.Find(id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            if (!CanManageEvent(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (!IsAttendanceOpen(suKien))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Sự kiện chưa được duyệt/chưa mở điểm danh.");
            }

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddSeconds(-1));
            return Json(new { url = CreateAttendanceUrl(id) }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Xuất danh sách sinh viên đăng ký sự kiện ra file Excel.
        /// </summary>
        /// <param name="id">Mã sự kiện</param>
        /// <returns>File Excel (.xlsx)</returns>
        public ActionResult XuatExcel(string id)
        {
            var suKien = db.SuKiens.Find(id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            if (!CanManageEvent(suKien))
            {
                return View("Forbidden");
            }

            var items = LoadRegistrations(id).ToList();
            var bytes = new EventReportService().CreateExcel(suKien, items);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSach_" + id + ".xlsx");
        }

        /// <summary>
        /// Xuất danh sách sinh viên đăng ký sự kiện ra file PDF.
        /// </summary>
        /// <param name="id">Mã sự kiện</param>
        /// <returns>File PDF (.pdf)</returns>
        public ActionResult XuatPdf(string id)
        {
            var suKien = db.SuKiens.Find(id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            if (!CanManageEvent(suKien))
            {
                return View("Forbidden");
            }

            var items = LoadRegistrations(id).ToList();
            var bytes = new EventReportService().CreatePdf(suKien, items);
            return File(bytes, "application/pdf", "DanhSach_" + id + ".pdf");
        }

        /// <summary>
        /// Gửi email nhắc nhở lịch sự kiện cho tất cả sinh viên đã đăng ký.
        /// </summary>
        /// <param name="id">Mã sự kiện</param>
        /// <returns>Redirect về chi tiết sự kiện kèm thông báo</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiNhacNho(string id)
        {
            var suKien = db.SuKiens.Find(id);
            if (suKien == null)
            {
                return HttpNotFound();
            }

            if (!CanManageEvent(suKien))
            {
                return View("Forbidden");
            }

            try
            {
                var emailService = new EmailService();
                foreach (var item in LoadRegistrations(id).ToList())
                {
                    var user = item.NguoiDung;
                    if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    {
                        continue;
                    }

                    emailService.Send(
                        user.Email,
                        "Nhắc lịch sự kiện: " + suKien.TenSuKien,
                        "<p>Xin chào " + HttpUtility.HtmlEncode(user.HoTen) + ",</p>" +
                        "<p>Bạn đã đăng ký tham gia sự kiện <strong>" + HttpUtility.HtmlEncode(suKien.TenSuKien) + "</strong>.</p>" +
                        "<p>Thời gian: " + suKien.ThoiGianBatDau.ToString("dd/MM/yyyy HH:mm") + "</p>" +
                        "<p>Địa điểm: " + HttpUtility.HtmlEncode(suKien.DiaDiem) + "</p>");
                }

                TempData["Message"] = "Đã gửi email nhắc nhở cho danh sách sinh viên có email.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Chưa gửi được email: " + ex.Message;
            }

            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Helper nạp các SelectList cần thiết cho View (Học kỳ, Năm học, Mức điểm...).
        /// </summary>
        private void LoadSelectLists(SuKien suKien)
        {
            ViewBag.MaHocKy = new SelectList(db.HocKies.OrderBy(x => x.TenHocKy), "MaHocKy", "TenHocKy", suKien.MaHocKy);
            ViewBag.MaMucDRL = new SelectList(db.MucDiemRenLuyens.OrderBy(x => x.TenMucDRL), "MaMucDRL", "TenMucDRL", suKien.MaMucDRL);
            ViewBag.MaNamHoc = new SelectList(db.NamHocs.OrderByDescending(x => x.MaNamHoc), "MaNamHoc", "TenNamHoc", suKien.MaNamHoc);
            ViewBag.MaTTSK = new SelectList(db.TrangThaiSuKiens.OrderBy(x => x.MaTTSK), "MaTTSK", "TenTTSK", suKien.MaTTSK);
            ViewBag.EventStatusName = db.TrangThaiSuKiens
                .Where(x => x.MaTTSK == suKien.MaTTSK)
                .Select(x => x.TenTTSK)
                .FirstOrDefault();
            ViewBag.CapToChucList = CurrentRole() == RoleKeys.ClassOfficer
                ? new SelectList(new[] { "Lớp" }, suKien.CapToChuc)
                : new SelectList(new[] { "Khoa", "Lớp" }, suKien.CapToChuc);
            var selectedParentId = EventHierarchyService.GetParentId(db, suKien.MaSuKien);
            ViewBag.MaSuKienCha = new SelectList(LoadParentEventOptions(suKien), "MaSuKien", "DisplayText", selectedParentId);
        }

        /// <summary>
        /// Lưu mối quan hệ sự kiện cha (nếu có).
        /// </summary>
        private void SaveParentEvent(string eventId)
        {
            EventHierarchyService.SaveParentId(db, eventId, Request["MaSuKienCha"]);
        }

        /// <summary>
        /// Tải danh sách các sự kiện có thể làm "sự kiện cha" (là các sự kiện do khoa tổ chức
        /// </summary>
        private List<ParentEventOption> LoadParentEventOptions(SuKien suKien)
        {
            EventHierarchyService.EnsureSchema(db);
            var facultyId = CurrentFacultyId();
            return db.SuKiens
                .Include(x => x.NguoiDung)
                .OrderByDescending(x => x.ThoiGianBatDau)
                .ToList()
                .Where(x =>
                    x.MaSuKien != suKien.MaSuKien &&
                    !IsClassLevel(x.CapToChuc) &&
                    (string.IsNullOrWhiteSpace(facultyId) || GetUserFacultyId(x.MaNguoiTao) == facultyId))
                .Select(x => new ParentEventOption
                {
                    MaSuKien = x.MaSuKien,
                    TenSuKien = x.TenSuKien
                })
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách đăng ký và điểm danh của một sự kiện.
        /// </summary>
        private IQueryable<DangKy_DiemDanh> LoadRegistrations(string eventId)
        {
            return db.DangKy_DiemDanh
                .Include(x => x.NguoiDung)
                .Include(x => x.TrangThaiDangKy)
                .Include(x => x.TrangThaiDiemDanh)
                .Where(x => x.MaSuKien == eventId)
                .OrderBy(x => x.NguoiDung.HoTen);
        }

        /// <summary>
        /// Kiểm tra người dùng hiện tại có quyền quản lý sự kiện này không.
        /// </summary>
        private bool CanManageEvent(SuKien suKien)
        {
            var role = CurrentRole();
            if (role == RoleKeys.Admin || role == RoleKeys.FacultyUnion)
            {
                return role == RoleKeys.Admin || IsSameFacultyEvent(suKien);
            }

            return role == RoleKeys.ClassOfficer && IsClassLevel(suKien.CapToChuc) && IsSameClassEvent(suKien);
        }

        /// <summary>
        /// Kiểm tra người dùng hiện tại có quyền xem chi tiết sự kiện này không.
        /// </summary>
        private bool CanViewEvent(SuKien suKien)
        {
            var role = CurrentRole();
            if (role == RoleKeys.Admin || role == RoleKeys.FacultyUnion)
            {
                return role == RoleKeys.Admin || IsSameFacultyEvent(suKien);
            }

            if (role != RoleKeys.ClassOfficer)
            {
                return false;
            }

            return IsClassLevel(suKien.CapToChuc)
                ? IsSameClassEvent(suKien)
                : IsSameFacultyEvent(suKien);
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của sự kiện dựa trên vai trò người tạo.
        /// </summary>
        private void ValidateEventTime(SuKien suKien)
        {
            if (suKien.ThoiGianBatDau >= suKien.ThoiGianKetThuc)
            {
                ModelState.AddModelError("ThoiGianKetThuc", "Thời gian kết thúc phải sau thời gian bắt đầu.");
            }

        }

        /// <summary>
        /// Trích xuất và gán thời gian bắt đầu/kết thúc từ Request form.
        /// </summary>
        private void ApplyEventDatesFromRequest(SuKien suKien)
        {
            var now = DateTime.Now;
            suKien.ThoiGianBatDau = ParseDateTimeLocal(Request["ThoiGianBatDau"], now);
            suKien.ThoiGianKetThuc = ParseDateTimeLocal(Request["ThoiGianKetThuc"], suKien.ThoiGianBatDau.AddHours(2));
        }

        /// <summary>
        /// Chuyển đổi chuỗi datetime-local từ form sang đối tượng DateTime.
        /// </summary>
        private DateTime ParseDateTimeLocal(string value, DateTime fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            DateTime parsed;
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed)
                ? parsed
                : fallback;
        }


        /// <summary>
        /// Tạo URL dùng cho việc điểm danh (bao gồm token bảo mật).
        /// </summary>
        private string CreateAttendanceUrl(string eventId)
        {
            var token = QrTokenService.CreateQrToken(eventId, DateTime.Now);
            var relativeUrl = Url.Action("DiemDanhSuKien", "SinhVien", new { id = eventId, token });
            var publicBaseUrl = (ConfigurationManager.AppSettings["PublicBaseUrl"] ?? "").Trim().TrimEnd('/');

            if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                return publicBaseUrl + relativeUrl;
            }

            return Url.Action("DiemDanhSuKien", "SinhVien", new { id = eventId, token }, Request.Url.Scheme);
        }

        /// <summary>
        /// Tự động sinh mã sự kiện mới.
        /// </summary>
        private string CreateEventId()
        {
            var next = db.SuKiens.Count() + 1;
            string id;

            do
            {
                id = "SK" + next.ToString("000");
                next++;
            }
            while (db.SuKiens.Any(x => x.MaSuKien == id));

            return id;
        }

        /// <summary>
        /// Tìm mã trạng thái sự kiện dựa trên tên hoặc từ khóa.
        /// </summary>
        private string FindEventStatus(string keyword)
        {
            // Danh sách ưu tiên các từ khóa để tránh nhận diện sai (ví dụ "mở" và "đóng" đều có chữ "ó")
            var statuses = db.TrangThaiSuKiens.ToList();
            TrangThaiSuKien match = null;

            if (keyword == "Đang mở đăng ký")
                match = statuses.FirstOrDefault(x => x.TenTTSK.Equals("Đang mở đăng ký", StringComparison.OrdinalIgnoreCase) || x.TenTTSK.Contains("Mở đăng ký") || x.TenTTSK.Contains("mở đăng ký"));
            else if (keyword == "Chờ kiểm duyệt")
                match = statuses.FirstOrDefault(x => x.TenTTSK.Contains("Chờ") || x.TenTTSK.Contains("chờ") || x.TenTTSK.Contains("duyệt"));
            else if (keyword == "Từ chối")
                match = statuses.FirstOrDefault(x => x.TenTTSK.Contains("Từ chối") || x.TenTTSK.Contains("từ chối") || x.TenTTSK.Contains("Hủy"));
            else if (keyword == "Đã kết thúc")
                match = statuses.FirstOrDefault(x => x.TenTTSK.Contains("Kết thúc") || x.TenTTSK.Contains("kết thúc") || x.TenTTSK.Contains("Đóng"));

            if (match == null)
            {
                match = statuses.FirstOrDefault(x => x.TenTTSK.Contains(keyword));
            }

            return match != null ? match.MaTTSK : db.TrangThaiSuKiens.Select(x => x.MaTTSK).FirstOrDefault();
        }

        /// <summary>
        /// Kiểm tra xem sự kiện có đang trong trạng thái "mở đăng ký" hay không.
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
        /// Kiểm tra chuỗi cấp tổ chức có phải là "Lớp" hay không.
        /// </summary>
        private bool IsClassLevel(string capToChuc)
        {
            return !string.IsNullOrWhiteSpace(capToChuc) &&
                (capToChuc.Contains("Lớp") || capToChuc.Equals("Lop", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Kiểm tra người dùng hiện tại có quyền duyệt sự kiện hay không.
        /// </summary>
        private bool CanApproveEvents()
        {
            var role = CurrentRole();
            return role == RoleKeys.Admin || role == RoleKeys.FacultyUnion;
        }

        private bool CanDeleteEvents()
        {
            return CurrentRole() == RoleKeys.Admin;
        }

        /// <summary>
        /// Kiểm tra xem sự kiện có đang chờ kiểm duyệt hay không.
        /// </summary>
        private bool IsPendingEventApproval(SuKien suKien)
        {
            if (suKien == null)
            {
                return false;
            }

            if (suKien.TrangThaiSuKien == null)
            {
                db.Entry(suKien).Reference(x => x.TrangThaiSuKien).Load();
            }

            var statusName = suKien.TrangThaiSuKien != null ? suKien.TrangThaiSuKien.TenTTSK : "";
            return ContainsAny(statusName, "Chờ", "chờ", "Cho", "cho", "kiểm");
        }

        /// <summary>
        /// Kiểm tra xem người dùng hiện tại có quyền hủy sự kiện hay không.
        /// </summary>
        private bool CanCancelEvent(SuKien suKien)
        {
            if (suKien.MaTTSK == "TTSK08") return false; // Đã hủy

            var role = CurrentRole();
            if (role == RoleKeys.Admin) return true; // Admin được hủy bất kỳ lúc nào

            if (role == RoleKeys.ClassOfficer)
            {
                // BCS Lớp chỉ được hủy khi chưa duyệt (Chờ kiểm duyệt hoặc Bản nháp)
                return IsPendingEventApproval(suKien) || suKien.MaTTSK == "TTSK01";
            }

            if (role == RoleKeys.FacultyUnion)
            {
                if (suKien.TrangThaiSuKien == null)
                {
                    db.Entry(suKien).Reference(x => x.TrangThaiSuKien).Load();
                }
                var statusName = suKien.TrangThaiSuKien != null ? suKien.TrangThaiSuKien.TenTTSK.ToLower() : "";

                // Chỉ được hủy khi sự kiện Đang mở đăng ký, Đóng đăng ký, Bản nháp, Chờ kiểm duyệt
                // Tức là khi sự kiện chưa bước vào giai đoạn Đang diễn ra hay Kết thúc
                return statusName.Contains("mở") || statusName.Contains("đóng") || statusName.Contains("chờ") || statusName.Contains("nháp");
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra chuỗi có chứa bất kỳ từ khóa nào trong danh sách hay không.
        /// </summary>
        private bool ContainsAny(string value, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return keywords.Any(value.Contains);
        }

        /// <summary>
        /// Kiểm tra sự kiện có thuộc về Khoa của người dùng hiện tại hay không.
        /// </summary>
        private bool IsSameFacultyEvent(SuKien suKien)
        {
            var facultyId = CurrentFacultyId();
            if (string.IsNullOrWhiteSpace(facultyId))
            {
                return CurrentRole() == RoleKeys.FacultyUnion;
            }

            if (suKien.NguoiDung == null)
            {
                db.Entry(suKien).Reference(x => x.NguoiDung).Load();
            }

            return suKien.NguoiDung != null && GetUserFacultyId(suKien.NguoiDung.MaNguoiDung) == facultyId;
        }

        /// <summary>
        /// Kiểm tra sự kiện có thuộc về Lớp của người dùng hiện tại hay không.
        /// </summary>
        private bool IsSameClassEvent(SuKien suKien)
        {
            var classId = CurrentClassId();
            if (string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            if (suKien.NguoiDung == null)
            {
                db.Entry(suKien).Reference(x => x.NguoiDung).Load();
            }

            return suKien.NguoiDung != null && suKien.NguoiDung.MaLop == classId;
        }

        /// <summary>
        /// Gửi thông báo cho những người có quyền duyệt khi có sự kiện mới đang chờ.
        /// </summary>
        private void NotifyApproversForPendingEvent(SuKien suKien, bool resubmitted = false)
        {
            if (CurrentRole() != RoleKeys.ClassOfficer)
            {
                return;
            }

            if (suKien.NguoiDung == null)
            {
                db.Entry(suKien).Reference(x => x.NguoiDung).Load();
            }

            var creatorFacultyId = GetUserFacultyId(suKien.MaNguoiTao);
            var creatorClassId = suKien.NguoiDung != null ? suKien.NguoiDung.MaLop : null;

            var notificationService = new NotificationService(db);
            var approvers = db.NguoiDungs.Include(x => x.QuyenHan).Include(x => x.LopHoc).ToList()
                .Where(x =>
                {
                    var role = RoleHelper.Normalize(x.MaQuyen, x.QuyenHan != null ? x.QuyenHan.TenQuyen : "");
                    // Admin hoặc BCH Khoa (cùng khoa)
                    if (role == RoleKeys.Admin || (role == RoleKeys.FacultyUnion && x.LopHoc != null && x.LopHoc.MaKhoa == creatorFacultyId))
                    {
                        return true;
                    }
                    // Cùng là cán bộ lớp trong cùng một lớp
                    if (role == RoleKeys.ClassOfficer && !string.IsNullOrEmpty(creatorClassId) && x.MaLop == creatorClassId)
                    {
                        return true;
                    }
                    return false;
                })
                .Select(x => x.MaNguoiDung)
                .Distinct()
                .ToList();

            foreach (var approverId in approvers)
            {
                if (approverId == suKien.MaNguoiTao) continue; // Không gửi cho chính mình

                if (resubmitted)
                {
                    notificationService.Notify(
                        approverId,
                        "Sự kiện được gửi duyệt lại",
                        "Sự kiện \"" + suKien.TenSuKien + "\" đang chờ kiểm duyệt.",
                        "/SuKiens#event-" + suKien.MaSuKien,
                        "EVENT_APPROVAL_NEEDED",
                        suKien.MaSuKien);
                    continue;
                }

                notificationService.NotifyOnce(
                    approverId,
                    "Có sự kiện mới cần duyệt",
                    "Sự kiện \"" + suKien.TenSuKien + "\" đang chờ kiểm duyệt.",
                    "/SuKiens#event-" + suKien.MaSuKien,
                    "EVENT_APPROVAL_NEEDED",
                    suKien.MaSuKien);
            }
        }

        /// <summary>
        /// Gửi thông báo cho cán bộ lớp khi có sự kiện mới cấp Khoa.
        /// </summary>
        private void NotifyClassOfficersForFacultyEvent(SuKien suKien)
        {
            if (IsClassLevel(suKien.CapToChuc))
            {
                return;
            }

            var creatorFacultyId = GetUserFacultyId(suKien.MaNguoiTao);
            if (string.IsNullOrWhiteSpace(creatorFacultyId))
            {
                return;
            }

            var notificationService = new NotificationService(db);
            var classOfficers = db.NguoiDungs.Include(x => x.QuyenHan).Include(x => x.LopHoc).ToList()
                .Where(x =>
                    x.LopHoc != null &&
                    x.LopHoc.MaKhoa == creatorFacultyId &&
                    RoleHelper.Normalize(x.MaQuyen, x.QuyenHan != null ? x.QuyenHan.TenQuyen : "") == RoleKeys.ClassOfficer)
                .Select(x => x.MaNguoiDung)
                .Distinct()
                .ToList();

            foreach (var officerId in classOfficers)
            {
                notificationService.NotifyOnce(
                    officerId,
                    "Có sự kiện cấp khoa mới",
                    "Sự kiện \"" + suKien.TenSuKien + "\" vừa được tạo ở cấp khoa.",
                    "/SuKiens/Details/" + suKien.MaSuKien,
                    "EVENT_FACULTY_NEW",
                    suKien.MaSuKien);
            }
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
        /// Lấy mã Khoa của một người dùng bất kỳ.
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
        /// Gửi email thông báo kết quả duyệt sự kiện cho người tạo.
        /// </summary>
        private void NotifyEventCreator(SuKien suKien, bool approved)
        {
            if (suKien.NguoiDung == null || string.IsNullOrWhiteSpace(suKien.NguoiDung.Email))
            {
                return;
            }

            try
            {
                var subject = approved ? "Sự kiện đã được duyệt" : "Sự kiện không được duyệt";
                var body = approved
                    ? "<p>Sự kiện <strong>" + HttpUtility.HtmlEncode(suKien.TenSuKien) + "</strong> đã được duyệt và mở đăng ký.</p>"
                    : "<p>Sự kiện <strong>" + HttpUtility.HtmlEncode(suKien.TenSuKien) + "</strong> không được duyệt.</p><p>Lý do: " + HttpUtility.HtmlEncode(suKien.LyDoHuy) + "</p>";

                new EmailService().Send(suKien.NguoiDung.Email, subject, body);
            }
            catch
            {
                TempData["Message"] = (TempData["Message"] ?? "") + " Không gửi được email thông báo do chưa cấu hình SMTP.";
            }
        }

        /// <summary>
        /// Lấy mã người dùng hiện tại từ Session hoặc Identity.
        /// </summary>
        private string CurrentUserId()
        {
            return (Session["MaNguoiDung"] ?? User.Identity.Name ?? "").ToString();
        }

        /// <summary>
        /// Lấy vai trò hiện tại của người dùng từ Session.
        /// </summary>
        private string CurrentRole()
        {
            return (Session["RoleKey"] ?? "").ToString();
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
    }
}

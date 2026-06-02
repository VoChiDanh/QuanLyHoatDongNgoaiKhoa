using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QL_HDNK.Models;
using QL_HDNK.Filters;
using QL_HDNK.Services;

namespace QL_HDNK.Controllers
{
    /// <summary>
    /// Controller quản lý danh sách Đăng ký và Điểm danh tham gia sự kiện.
    /// Cho phép xem danh sách, điểm danh thủ công, duyệt/từ chối minh chứng QR.
    /// </summary>
    [RoleAuthorize(RoleKeys.Admin, RoleKeys.FacultyUnion, RoleKeys.ClassOfficer)]
    public class DangKy_DiemDanhController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các lượt đăng ký và điểm danh với các bộ lọc.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (mã SV, tên SV, tên sự kiện)</param>
        /// <param name="maSuKien">Mã sự kiện cần lọc</param>
        /// <param name="maTTDK">Mã trạng thái đăng ký cần lọc</param>
        /// <param name="maTTDD">Mã trạng thái điểm danh cần lọc</param>
        /// <returns>View danh sách đăng ký - điểm danh</returns>
        public ActionResult Index(string search, string maSuKien, string maTTDK, string maTTDD)
        {
            var role = CurrentRole();
            var userId = CurrentUserId();
            var facultyId = CurrentFacultyId();
            var classId = CurrentClassId();

            var query = db.DangKy_DiemDanh
                .Include(d => d.NguoiDung)
                .Include(d => d.SuKien)
                .Include(d => d.TrangThaiDangKy)
                .Include(d => d.TrangThaiDiemDanh)
                .Include(d => d.NguoiDung1)
                .AsQueryable();

            // Phân quyền dữ liệu: Chỉ thấy những gì mình có quyền quản lý hoặc liên quan
            if (role == RoleKeys.FacultyUnion)
            {
                query = string.IsNullOrWhiteSpace(facultyId)
                    ? query.Where(d => false)
                    : query.Where(d => d.SuKien.NguoiDung.LopHoc.MaKhoa == facultyId || d.NguoiDung.LopHoc.MaKhoa == facultyId);
            }
            else if (role == RoleKeys.ClassOfficer)
            {
                query = string.IsNullOrWhiteSpace(classId)
                    ? query.Where(d => false)
                    : query.Where(d => (d.SuKien.CapToChuc == "Lớp" && d.SuKien.NguoiDung.MaLop == classId) || (d.SuKien.CapToChuc != "Lớp" && d.NguoiDung.MaLop == classId));
            }

            // Áp dụng tìm kiếm theo từ khóa
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => 
                    d.NguoiDung.MaSinhVien.Contains(search) || 
                    d.NguoiDung.HoTen.Contains(search) || 
                    d.SuKien.TenSuKien.Contains(search));
            }

            // Lọc theo mã sự kiện
            if (!string.IsNullOrEmpty(maSuKien))
            {
                query = query.Where(d => d.MaSuKien == maSuKien);
            }

            // Lọc theo trạng thái đăng ký
            if (!string.IsNullOrEmpty(maTTDK))
            {
                query = query.Where(d => d.MaTTDK == maTTDK);
            }

            // Lọc theo trạng thái điểm danh
            if (!string.IsNullOrEmpty(maTTDD))
            {
                query = query.Where(d => d.MaTTDD == maTTDD);
            }

            // Chuẩn bị các danh sách cho dropdown lọc trong View
            var eventQuery = db.SuKiens.AsQueryable();
            if (role == RoleKeys.FacultyUnion)
            {
                eventQuery = eventQuery.Where(s => s.NguoiDung.LopHoc.MaKhoa == facultyId);
            }
            else if (role == RoleKeys.ClassOfficer)
            {
                // Cán bộ lớp: Thấy sự kiện của Lớp mình VÀ sự kiện của Khoa mình
                eventQuery = eventQuery.Where(s =>
                    (s.CapToChuc == "Lớp" && s.NguoiDung.MaLop == classId) ||
                    s.NguoiDung.LopHoc.MaKhoa == facultyId
                );
            }

            ViewBag.MaSuKien = new SelectList(eventQuery.OrderByDescending(s => s.ThoiGianBatDau).Take(50), "MaSuKien", "TenSuKien", maSuKien);
            ViewBag.MaTTDK = new SelectList(db.TrangThaiDangKies.OrderBy(x => x.TenTTDK), "MaTTDK", "TenTTDK", maTTDK);
            ViewBag.MaTTDD = new SelectList(db.TrangThaiDiemDanhs.OrderBy(x => x.TenTTDD), "MaTTDD", "TenTTDD", maTTDD);
            ViewBag.CurrentSearch = search;

            var result = query.OrderByDescending(d => d.ThoiGianDangKy).ToList();
            return View(result);
        }

        /// <summary>
        /// Điểm danh thủ công cho một sinh viên.
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>Redirect về trang Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DiemDanhThuCong(string id, HttpPostedFileBase fileMinhChung)
        {
            //Tìm bản ghi Đăng ký-Điểm danh trong db
            var item = db.DangKy_DiemDanh.Find(id);
            if (item == null) return HttpNotFound();

            // Kiểm tra quyền quản lý lượt đăng ký này
            if (!CanManageRegistration(item))
            {
                TempData["Message"] = "Bạn không có quyền điểm danh cho lượt đăng ký này.";
                return RedirectToAction("Index");
            }
            // Xử lý lưu file ảnh minh chứng (nếu có tải lên)
            if (fileMinhChung != null && fileMinhChung.ContentLength > 0)
            {
                // Xác định thư mục lưu trữ trên server (ví dụ: thư mục /Uploads/MinhChung)
                string uploadDir = "~/Uploads/MinhChung/";
                string physicalPath = Server.MapPath(uploadDir);

                // Tạo thư mục nếu chưa tồn tại
                if (!System.IO.Directory.Exists(physicalPath))
                {
                    System.IO.Directory.CreateDirectory(physicalPath);
                }

                // Tạo tên file duy nhất (Dùng ID + Thời gian để tránh trùng lặp tên file)
                string fileExtension = System.IO.Path.GetExtension(fileMinhChung.FileName);
                string fileName = "DD_" + id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + fileExtension;
                string fullPath = System.IO.Path.Combine(physicalPath, fileName);

                // Lưu file vật lý lên server
                fileMinhChung.SaveAs(fullPath);

                // Cập nhật đường dẫn vào database 
                // Thêm tiền tố "QRPHOTO:" để View của bạn nhận diện đúng định dạng ảnh
                item.MinhChung = "QRPHOTO:" + Url.Content(uploadDir + fileName);
            }
            // Cập nhật thông tin điểm danh
            item.ThoiGianDiemDanh = DateTime.Now;
            item.MaTTDD = FindAttendanceStatus(true);
            item.MaTTDK = FindRegistrationStatus("Đã tham gia") ?? FindRegistrationStatus("Đã xác nhận") ?? item.MaTTDK;
            item.NguoiDuyet = CurrentUserId();
            item.ThoiGianDuyet = DateTime.Now;
            item.GhiChu = (string.IsNullOrEmpty(item.GhiChu) ? "" : item.GhiChu + " | ") + 
                         "Cán bộ điểm danh thủ công lúc " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ".";
            
            db.SaveChanges();

            TempData["Message"] = "Đã điểm danh thủ công cho sinh viên.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Phê duyệt minh chứng điểm danh qua QR (dùng khi sinh viên không điểm danh tại chỗ được).
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>Redirect về trang Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DuyetQr(string id)
        {
            var item = db.DangKy_DiemDanh.Find(id);
            if (item == null)
            {
                return HttpNotFound();
            }


            item.ThoiGianDiemDanh = DateTime.Now;
            item.MaTTDD = FindAttendanceStatus(true);
            item.MaTTDK = FindRegistrationStatus("Đã tham gia") ?? FindRegistrationStatus("Đã xác nhận") ?? item.MaTTDK;
            item.NguoiDuyet = CurrentUserId();
            item.ThoiGianDuyet = DateTime.Now;
            item.GhiChu = "Đã duyệt điểm danh QR lúc " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ".";
            db.SaveChanges();

            // Xử lý thông báo cho sinh viên
            var notificationService = new NotificationService(db);
            notificationService.MarkRelatedRead("ATTENDANCE_PENDING", item.MaDangKy);
            notificationService.NotifyAttendanceEvidence(item, true);
            TempData["Message"] = "Đã duyệt điểm danh QR.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Từ chối minh chứng điểm danh qua QR.
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>Redirect về trang Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoiQr(string id)
        {
            var item = db.DangKy_DiemDanh.Find(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            if (!CanManageRegistration(item))
            {
                TempData["Message"] = "Bạn không có quyền từ chối minh chứng này.";
                return RedirectToAction("Index");
            }

            if (!IsPendingQr(item))
            {
                new NotificationService(db).MarkRelatedRead("ATTENDANCE_PENDING", item.MaDangKy);
                TempData["Message"] = "Minh chứng này đã được xử lý hoặc không còn ở trạng thái chờ duyệt.";
                return RedirectToAction("Index");
            }

            // Cập nhật trạng thái từ chối
            item.ThoiGianDiemDanh = null;
            item.MaTTDD = FindRejectedAttendanceStatus();
            item.NguoiDuyet = CurrentUserId();
            item.ThoiGianDuyet = DateTime.Now;
            item.GhiChu = "Từ chối điểm danh QR lúc " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ".";
            db.SaveChanges();

            // Xử lý thông báo cho sinh viên
            var notificationService = new NotificationService(db);
            notificationService.MarkRelatedRead("ATTENDANCE_PENDING", item.MaDangKy);
            notificationService.NotifyAttendanceEvidence(item, false);
            TempData["Message"] = "Đã từ chối điểm danh QR.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị chi tiết một lượt đăng ký.
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>View chi tiết</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DangKy_DiemDanh dangKy_DiemDanh = db.DangKy_DiemDanh.Find(id);
            if (dangKy_DiemDanh == null)
            {
                return HttpNotFound();
            }
            return View(dangKy_DiemDanh);
        }

        /// <summary>
        /// Hiển thị form tạo mới một lượt đăng ký/điểm danh (thường cho quản trị viên).
        /// </summary>
        /// <returns>View form tạo mới</returns>
        public ActionResult Create()
        {
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien");
            ViewBag.MaSuKien = new SelectList(db.SuKiens, "MaSuKien", "TenSuKien");
            ViewBag.MaTTDK = new SelectList(db.TrangThaiDangKies, "MaTTDK", "TenTTDK");
            ViewBag.MaTTDD = new SelectList(db.TrangThaiDiemDanhs, "MaTTDD", "TenTTDD");
            ViewBag.NguoiDuyet = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien");
            return View(new DangKy_DiemDanh { ThoiGianDangKy = DateTime.Now });
        }

        /// <summary>
        /// Xử lý lưu thông tin lượt đăng ký/điểm danh mới.
        /// </summary>
        /// <param name="dangKy_DiemDanh">Đối tượng dữ liệu từ form</param>
        /// <returns>Redirect về Index hoặc hiển thị lại form nếu lỗi</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaDangKy,MaSuKien,MaNguoiDung,ThoiGianDangKy,MaTTDK,ThoiGianDiemDanh,MinhChung,MaTTDD,NguoiDuyet,ThoiGianDuyet,GhiChu")] DangKy_DiemDanh dangKy_DiemDanh)
        {
            if (dangKy_DiemDanh.ThoiGianDangKy == DateTime.MinValue)
            {
                dangKy_DiemDanh.ThoiGianDangKy = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                db.DangKy_DiemDanh.Add(dangKy_DiemDanh);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", dangKy_DiemDanh.MaNguoiDung);
            ViewBag.MaSuKien = new SelectList(db.SuKiens, "MaSuKien", "TenSuKien", dangKy_DiemDanh.MaSuKien);
            ViewBag.MaTTDK = new SelectList(db.TrangThaiDangKies, "MaTTDK", "TenTTDK", dangKy_DiemDanh.MaTTDK);
            ViewBag.MaTTDD = new SelectList(db.TrangThaiDiemDanhs, "MaTTDD", "TenTTDD", dangKy_DiemDanh.MaTTDD);
            ViewBag.NguoiDuyet = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", dangKy_DiemDanh.NguoiDuyet);
            return View(dangKy_DiemDanh);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa thông tin lượt đăng ký/điểm danh.
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>View form chỉnh sửa</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DangKy_DiemDanh dangKy_DiemDanh = db.DangKy_DiemDanh.Find(id);
            if (dangKy_DiemDanh == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", dangKy_DiemDanh.MaNguoiDung);
            ViewBag.MaSuKien = new SelectList(db.SuKiens, "MaSuKien", "TenSuKien", dangKy_DiemDanh.MaSuKien);
            ViewBag.MaTTDK = new SelectList(db.TrangThaiDangKies, "MaTTDK", "TenTTDK", dangKy_DiemDanh.MaTTDK);
            ViewBag.MaTTDD = new SelectList(db.TrangThaiDiemDanhs, "MaTTDD", "TenTTDD", dangKy_DiemDanh.MaTTDD);
            ViewBag.NguoiDuyet = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", dangKy_DiemDanh.NguoiDuyet);
            return View(dangKy_DiemDanh);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin lượt đăng ký/điểm danh.
        /// </summary>
        /// <param name="dangKy_DiemDanh">Đối tượng dữ liệu đã sửa</param>
        /// <returns>Redirect về Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaDangKy,MaSuKien,MaNguoiDung,ThoiGianDangKy,MaTTDK,ThoiGianDiemDanh,MinhChung,MaTTDD,NguoiDuyet,ThoiGianDuyet,GhiChu")] DangKy_DiemDanh dangKy_DiemDanh)
        {
            if (ModelState.IsValid)
            {
                db.Entry(dangKy_DiemDanh).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", dangKy_DiemDanh.MaNguoiDung);
            ViewBag.MaSuKien = new SelectList(db.SuKiens, "MaSuKien", "TenSuKien", dangKy_DiemDanh.MaSuKien);
            ViewBag.MaTTDK = new SelectList(db.TrangThaiDangKies, "MaTTDK", "TenTTDK", dangKy_DiemDanh.MaTTDK);
            ViewBag.MaTTDD = new SelectList(db.TrangThaiDiemDanhs, "MaTTDD", "TenTTDD", dangKy_DiemDanh.MaTTDD);
            ViewBag.NguoiDuyet = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", dangKy_DiemDanh.NguoiDuyet);
            return View(dangKy_DiemDanh);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa lượt đăng ký.
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>View xác nhận xóa</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DangKy_DiemDanh dangKy_DiemDanh = db.DangKy_DiemDanh.Find(id);
            if (dangKy_DiemDanh == null)
            {
                return HttpNotFound();
            }
            return View(dangKy_DiemDanh);
        }

        /// <summary>
        /// Xử lý xóa lượt đăng ký khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã lượt đăng ký</param>
        /// <returns>Redirect về Index</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            DangKy_DiemDanh dangKy_DiemDanh = db.DangKy_DiemDanh.Find(id);
            if (dangKy_DiemDanh != null)
            {
                var thongBaoList = db.ThongBaos.Where(t => t.MaLienQuan == id).ToList();
                if (thongBaoList.Any())
                {
                    db.ThongBaos.RemoveRange(thongBaoList);
                }
                
                db.DangKy_DiemDanh.Remove(dangKy_DiemDanh);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giải phóng các tài nguyên kết nối cơ sở dữ liệu.
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
        /// Lấy mã người dùng hiện tại từ Session hoặc Identity.
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
        /// Kiểm tra người dùng hiện tại có quyền quản lý lượt đăng ký này hay không.
        /// </summary>
        private bool CanManageRegistration(DangKy_DiemDanh item)
        {
            var role = CurrentRole();
            if (role == RoleKeys.Admin)
            {
                return true;
            }

            if (item.NguoiDung == null)
            {
                db.Entry(item).Reference(x => x.NguoiDung).Load();
            }

            if (role == RoleKeys.FacultyUnion)
            {
                return IsSameFacultyRegistration(item);
            }

            if (role != RoleKeys.ClassOfficer)
            {
                return false;
            }

            var classId = CurrentClassId();
            if (string.IsNullOrEmpty(classId)) return false;

            // Cho phép nếu sinh viên thuộc lớp của cán bộ lớp này
            if (item.NguoiDung != null && item.NguoiDung.MaLop == classId)
            {
                return true;
            }

            // Hoặc cho phép nếu là sự kiện cấp lớp do cán bộ lớp này (hoặc người trong lớp) tạo
            if (item.SuKien == null)
            {
                db.Entry(item).Reference(x => x.SuKien).Load();
            }

            return item.SuKien != null && IsClassLevel(item.SuKien.CapToChuc) && IsSameClassRegistration(item);
        }

        /// <summary>
        /// Kiểm tra lượt đăng ký này có thuộc về Khoa mà người dùng đang quản lý hay không.
        /// </summary>
        private bool IsSameFacultyRegistration(DangKy_DiemDanh item)
        {
            var facultyId = CurrentFacultyId();
            if (string.IsNullOrWhiteSpace(facultyId))
            {
                return CurrentRole() == RoleKeys.FacultyUnion;
            }

            if (item.SuKien == null)
            {
                db.Entry(item).Reference(x => x.SuKien).Load();
            }

            if (item.NguoiDung == null)
            {
                db.Entry(item).Reference(x => x.NguoiDung).Load();
            }

            return item.SuKien != null &&
                (GetUserFacultyId(item.SuKien.MaNguoiTao) == facultyId ||
                 (item.NguoiDung != null && GetUserFacultyId(item.NguoiDung.MaNguoiDung) == facultyId));
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
        /// Kiểm tra lượt đăng ký này có thuộc về Lớp mà người dùng đang quản lý hay không.
        /// </summary>
        private bool IsSameClassRegistration(DangKy_DiemDanh item)
        {
            var classId = CurrentClassId();
            if (string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            if (item.SuKien == null)
            {
                db.Entry(item).Reference(x => x.SuKien).Load();
            }

            return item.SuKien != null && GetUserClassId(item.SuKien.MaNguoiTao) == classId;
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
        /// Lấy mã Lớp của một người dùng bất kỳ.
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
        /// Kiểm tra lượt đăng ký có đang chờ duyệt minh chứng QR hay không.
        /// </summary>
        private bool IsPendingQr(DangKy_DiemDanh item)
        {
            if (item.ThoiGianDiemDanh.HasValue || string.IsNullOrWhiteSpace(item.MinhChung) || (!item.MinhChung.StartsWith("QR:") && !item.MinhChung.StartsWith("QRPHOTO:")))
            {
                return false;
            }

            if (item.TrangThaiDiemDanh == null)
            {
                db.Entry(item).Reference(x => x.TrangThaiDiemDanh).Load();
            }

            var statusName = item.TrangThaiDiemDanh != null ? item.TrangThaiDiemDanh.TenTTDD : "";
            var note = item.GhiChu ?? "";
            return !statusName.Contains("Từ chối") &&
                   !statusName.Contains("từ chối") &&
                   !note.Contains("Từ chối") &&
                   !note.Contains("từ chối");
        }

        /// <summary>
        /// Tìm mã trạng thái đăng ký phù hợp dựa trên từ khóa.
        /// </summary>
        private string FindRegistrationStatus(string keyword)
        {
            var statuses = db.TrangThaiDangKies.ToList();
            TrangThaiDangKy match = null;

            if (keyword == "Đã tham gia" || keyword == "Đã xác nhận")
            {
                match = statuses.FirstOrDefault(x => x.TenTTDK.Equals("Đã xác nhận tham gia", StringComparison.OrdinalIgnoreCase))
                        ?? statuses.FirstOrDefault(x => x.TenTTDK.Contains("xác nhận") || x.TenTTDK.Contains("tham gia"))
                        ?? statuses.FirstOrDefault(x => x.TenTTDK.Contains("thành công"));
            }
            else
            {
                match = statuses.FirstOrDefault(x => x.TenTTDK.Contains(keyword));
            }

            return match != null ? match.MaTTDK : null;
        }

        /// <summary>
        /// Tìm mã trạng thái điểm danh phù hợp.
        /// </summary>
        private string FindAttendanceStatus(bool attended)
        {
            var statuses = db.TrangThaiDiemDanhs.ToList();
            TrangThaiDiemDanh status = null;

            if (attended)
            {
                status = statuses.FirstOrDefault(x => x.TenTTDD.Equals("Có mặt", StringComparison.OrdinalIgnoreCase))
                         ?? statuses.FirstOrDefault(x => x.TenTTDD.Contains("Đã") || x.TenTTDD.Contains("Có") || x.TenTTDD.Contains("hợp lệ"));
            }
            else
            {
                status = statuses.FirstOrDefault(x => x.TenTTDD.Equals("Chưa điểm danh", StringComparison.OrdinalIgnoreCase))
                         ?? statuses.FirstOrDefault(x => x.TenTTDD.Contains("Chưa") || x.TenTTDD.Contains("chưa"));
            }

            return status != null ? status.MaTTDD : db.TrangThaiDiemDanhs.Select(x => x.MaTTDD).FirstOrDefault();
        }

        /// <summary>
        /// Lấy mã trạng thái điểm danh "Từ chối".
        /// </summary>
        private string FindRejectedAttendanceStatus()
        {
            var status = db.TrangThaiDiemDanhs.FirstOrDefault(x => x.TenTTDD.Contains("từ chối") || x.TenTTDD.Contains("Từ chối"));
            return status != null ? status.MaTTDD : FindAttendanceStatus(false);
        }
    }
}

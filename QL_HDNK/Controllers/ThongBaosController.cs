using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using QL_HDNK.Filters;
using QL_HDNK.Models;
using QL_HDNK.Services;

namespace QL_HDNK.Controllers
{
    [Authorize]
    [RoleAuthorize(RoleKeys.Admin, RoleKeys.FacultyUnion, RoleKeys.ClassOfficer, RoleKeys.Student)]
    public class ThongBaosController : Controller
    {
        private readonly QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách thông báo của người dùng hiện tại.
        /// </summary>
        /// <returns>View danh sách thông báo.</returns>
        public ActionResult Index()
        {
            var userId = CurrentUserId();
            var items = new NotificationService(db).ListForUser(userId);
            ViewBag.ActionableEventIds = LoadActionableEventIds(items);
            ViewBag.ActionableAttendanceIds = LoadActionableAttendanceIds(items);
            return View(items);
        }

        /// <summary>
        /// Action trả về badge hiển thị số lượng thông báo chưa đọc.
        /// </summary>
        /// <returns>PartialView chứa badge thông báo.</returns>
        [ChildActionOnly]
        public ActionResult Badge()
        {
            ViewBag.UnreadCount = new NotificationService(db).CountUnread(CurrentUserId());
            return PartialView("_Badge");
        }

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc.
        /// </summary>
        /// <param name="id">Mã thông báo.</param>
        /// <param name="returnUrl">URL để quay lại sau khi thực hiện.</param>
        /// <returns>Chuyển hướng về trang Index hoặc returnUrl.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkRead(string id, string returnUrl)
        {
            new NotificationService(db).MarkRead(CurrentUserId(), id);
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc.
        /// </summary>
        /// <returns>Chuyển hướng về trang Index.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllRead()
        {
            new NotificationService(db).MarkAllRead(CurrentUserId());
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Lấy ID của người dùng hiện tại từ Session hoặc Identity.
        /// </summary>
        /// <returns>Mã người dùng.</returns>
        private string CurrentUserId()
        {
            return (Session["MaNguoiDung"] ?? User.Identity.Name ?? "").ToString();
        }

        /// <summary>
        /// Lấy quyền của người dùng hiện tại.
        /// </summary>
        /// <returns>Key quyền hạn.</returns>
        private string CurrentRole()
        {
            return (Session["RoleKey"] ?? "").ToString();
        }

        /// <summary>
        /// Tải danh sách các ID sự kiện mà người dùng có quyền xử lý phê duyệt.
        /// </summary>
        /// <param name="notifications">Danh sách thông báo.</param>
        /// <returns>Danh sách mã sự kiện có thể xử lý.</returns>
        private List<string> LoadActionableEventIds(IEnumerable<ThongBaoViewModel> notifications)
        {
            if (!CanApproveEvents())
            {
                return new List<string>();
            }

            var ids = notifications
                .Where(x => x.Loai == "EVENT_APPROVAL_NEEDED" && !string.IsNullOrWhiteSpace(x.MaLienQuan))
                .Select(x => x.MaLienQuan)
                .Distinct()
                .ToList();

            if (!ids.Any())
            {
                return ids;
            }

            return db.SuKiens
                .Include(x => x.NguoiDung)
                .Include(x => x.TrangThaiSuKien)
                .Where(x => ids.Contains(x.MaSuKien))
                .ToList()
                .Where(x => CanManageEvent(x) && IsPendingEventApproval(x))
                .Select(x => x.MaSuKien)
                .ToList();
        }

        /// <summary>
        /// Tải danh sách các ID đăng ký điểm danh mà người dùng có quyền xử lý.
        /// </summary>
        /// <param name="notifications">Danh sách thông báo.</param>
        /// <returns>Danh sách mã đăng ký có thể xử lý.</returns>
        private List<string> LoadActionableAttendanceIds(IEnumerable<ThongBaoViewModel> notifications)
        {
            var ids = notifications
                .Where(x => x.Loai == "ATTENDANCE_PENDING" && !string.IsNullOrWhiteSpace(x.MaLienQuan))
                .Select(x => x.MaLienQuan)
                .Distinct()
                .ToList();

            if (!ids.Any())
            {
                return ids;
            }

            return db.DangKy_DiemDanh
                .Include(x => x.SuKien)
                .Include(x => x.SuKien.NguoiDung)
                .Include(x => x.NguoiDung)
                .Include(x => x.TrangThaiDiemDanh)
                .Where(x => ids.Contains(x.MaDangKy))
                .ToList()
                .Where(x => CanManageRegistration(x) && IsPendingQr(x))
                .Select(x => x.MaDangKy)
                .ToList();
        }

        /// <summary>
        /// Kiểm tra người dùng có quyền phê duyệt sự kiện không.
        /// </summary>
        /// <returns>True nếu có quyền.</returns>
        private bool CanApproveEvents()
        {
            var role = CurrentRole();
            return role == RoleKeys.Admin || role == RoleKeys.FacultyUnion;
        }

        /// <summary>
        /// Kiểm tra người dùng có quyền quản lý sự kiện cụ thể không.
        /// </summary>
        /// <param name="suKien">Đối tượng sự kiện.</param>
        /// <returns>True nếu có quyền.</returns>
        private bool CanManageEvent(SuKien suKien)
        {
            var role = CurrentRole();
            if (role == RoleKeys.Admin)
            {
                return true;
            }

            return role == RoleKeys.FacultyUnion && IsSameFacultyEvent(suKien);
        }

        /// <summary>
        /// Kiểm tra sự kiện có đang chờ phê duyệt không.
        /// </summary>
        /// <param name="suKien">Đối tượng sự kiện.</param>
        /// <returns>True nếu đang chờ phê duyệt.</returns>
        private bool IsPendingEventApproval(SuKien suKien)
        {
            var statusName = suKien.TrangThaiSuKien != null ? suKien.TrangThaiSuKien.TenTTSK : "";
            return ContainsAny(statusName, "Chờ", "chờ", "Cho", "cho", "kiểm");
        }

        /// <summary>
        /// Kiểm tra người dùng có quyền quản lý đơn đăng ký/điểm danh cụ thể không.
        /// </summary>
        /// <param name="item">Đối tượng đăng ký điểm danh.</param>
        /// <returns>True nếu có quyền.</returns>
        private bool CanManageRegistration(DangKy_DiemDanh item)
        {
            var role = CurrentRole();
            if (role == RoleKeys.Admin)
            {
                return true;
            }

            if (role == RoleKeys.FacultyUnion)
            {
                return IsSameFacultyRegistration(item);
            }

            return role == RoleKeys.ClassOfficer && IsClassLevel(item.SuKien != null ? item.SuKien.CapToChuc : null) && IsSameClassRegistration(item);
        }

        /// <summary>
        /// Kiểm tra đơn đăng ký có đang chờ xác nhận QR không.
        /// </summary>
        /// <param name="item">Đối tượng đăng ký điểm danh.</param>
        /// <returns>True nếu đang chờ xác nhận QR.</returns>
        private bool IsPendingQr(DangKy_DiemDanh item)
        {
            if (item.ThoiGianDiemDanh.HasValue ||
                string.IsNullOrWhiteSpace(item.MinhChung) ||
                (!item.MinhChung.StartsWith("QR:") && !item.MinhChung.StartsWith("QRPHOTO:")))
            {
                return false;
            }

            var statusName = item.TrangThaiDiemDanh != null ? item.TrangThaiDiemDanh.TenTTDD : "";
            var note = item.GhiChu ?? "";
            return !ContainsAny(statusName, "Từ chối", "từ chối") && !ContainsAny(note, "Từ chối", "từ chối");
        }

        /// <summary>
        /// Kiểm tra sự kiện có thuộc cùng khoa với người dùng hiện tại không.
        /// </summary>
        /// <param name="suKien">Đối tượng sự kiện.</param>
        /// <returns>True nếu cùng khoa.</returns>
        private bool IsSameFacultyEvent(SuKien suKien)
        {
            var facultyId = CurrentFacultyId();
            if (string.IsNullOrWhiteSpace(facultyId))
            {
                return CurrentRole() == RoleKeys.FacultyUnion;
            }

            return suKien != null &&
                suKien.NguoiDung != null &&
                GetUserFacultyId(suKien.NguoiDung.MaNguoiDung) == facultyId;
        }

        /// <summary>
        /// Kiểm tra đơn đăng ký có liên quan đến cùng khoa với người dùng không.
        /// </summary>
        /// <param name="item">Đối tượng đăng ký điểm danh.</param>
        /// <returns>True nếu cùng khoa.</returns>
        private bool IsSameFacultyRegistration(DangKy_DiemDanh item)
        {
            var facultyId = CurrentFacultyId();
            if (string.IsNullOrWhiteSpace(facultyId))
            {
                return CurrentRole() == RoleKeys.FacultyUnion;
            }

            return item.SuKien != null &&
                (GetUserFacultyId(item.SuKien.MaNguoiTao) == facultyId ||
                 (item.NguoiDung != null && GetUserFacultyId(item.NguoiDung.MaNguoiDung) == facultyId));
        }

        /// <summary>
        /// Kiểm tra đơn đăng ký có thuộc cùng lớp với người dùng không.
        /// </summary>
        /// <param name="item">Đối tượng đăng ký điểm danh.</param>
        /// <returns>True nếu cùng lớp.</returns>
        private bool IsSameClassRegistration(DangKy_DiemDanh item)
        {
            var classId = CurrentClassId();
            return item.SuKien != null &&
                !string.IsNullOrWhiteSpace(classId) &&
                GetUserClassId(item.SuKien.MaNguoiTao) == classId;
        }

        /// <summary>
        /// Lấy mã khoa của người dùng hiện tại.
        /// </summary>
        /// <returns>Mã khoa.</returns>
        private string CurrentFacultyId()
        {
            return GetUserFacultyId(CurrentUserId());
        }

        /// <summary>
        /// Lấy mã lớp của người dùng hiện tại.
        /// </summary>
        /// <returns>Mã lớp.</returns>
        private string CurrentClassId()
        {
            return GetUserClassId(CurrentUserId());
        }

        /// <summary>
        /// Lấy mã khoa của một người dùng bất kỳ.
        /// </summary>
        /// <param name="userId">Mã người dùng.</param>
        /// <returns>Mã khoa.</returns>
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
        /// Lấy mã lớp của một người dùng bất kỳ.
        /// </summary>
        /// <param name="userId">Mã người dùng.</param>
        /// <returns>Mã lớp.</returns>
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
        /// Kiểm tra cấp tổ chức có phải cấp Lớp không.
        /// </summary>
        /// <param name="capToChuc">Chuỗi mô tả cấp tổ chức.</param>
        /// <returns>True nếu là cấp lớp.</returns>
        private bool IsClassLevel(string capToChuc)
        {
            return ContainsAny(capToChuc, "Lớp", "Lop");
        }

        /// <summary>
        /// Kiểm tra chuỗi có chứa bất kỳ từ khóa nào không.
        /// </summary>
        /// <param name="value">Giá trị chuỗi cần kiểm tra.</param>
        /// <param name="keywords">Danh sách từ khóa.</param>
        /// <returns>True nếu có chứa ít nhất một từ khóa.</returns>
        private bool ContainsAny(string value, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return keywords.Any(value.Contains);
        }

        /// <summary>
        /// Giải phóng tài nguyên.
        /// </summary>
        /// <param name="disposing">True nếu đang giải phóng tài nguyên.</param>
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

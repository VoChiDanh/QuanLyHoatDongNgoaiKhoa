using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using QL_HDNK.Filters;
using QL_HDNK.Models;

namespace QL_HDNK.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị trang đăng nhập.
        /// </summary>
        /// <param name="returnUrl">URL để quay lại sau khi đăng nhập thành công.</param>
        /// <returns>View đăng nhập hoặc chuyển hướng nếu đã đăng nhập.</returns>
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (Request.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Xử lý yêu cầu đăng nhập.
        /// </summary>
        /// <param name="model">Dữ liệu đăng nhập từ người dùng.</param>
        /// <param name="returnUrl">URL để quay lại sau khi đăng nhập thành công.</param>
        /// <returns>Chuyển hướng đến trang chủ hoặc URL quay lại, hoặc hiển thị lỗi nếu đăng nhập thất bại.</returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var hashedPassword = HashPassword(model.MatKhau);
            var legacyMd5Password = HashLegacyMd5(model.MatKhau);
            var user = db.NguoiDungs
                .Include("QuyenHan")
                .FirstOrDefault(x =>
                    (x.MaSinhVien == model.TaiKhoan || x.Email == model.TaiKhoan || x.MaNguoiDung == model.TaiKhoan) &&
                    (x.MatKhau == model.MatKhau || x.MatKhau == hashedPassword || x.MatKhau == legacyMd5Password));

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            FormsAuthentication.SetAuthCookie(user.MaNguoiDung, model.GhiNho);
            SaveUserSession(user);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Hiển thị trang đăng ký tài khoản mới.
        /// </summary>
        /// <returns>View đăng ký tài khoản.</returns>
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (Request.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            LoadRegisterSelectLists();
            return View(new RegisterViewModel { NgaySinh = DateTime.Today });
        }

        /// <summary>
        /// Xử lý yêu cầu đăng ký tài khoản mới.
        /// </summary>
        /// <param name="model">Dữ liệu đăng ký từ người dùng.</param>
        /// <returns>Chuyển hướng đến trang chủ sau khi đăng ký thành công, hoặc hiển thị lỗi.</returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (db.NguoiDungs.Any(x => x.MaSinhVien == model.MaSinhVien || x.Email == model.Email))
            {
                ModelState.AddModelError("", "Mã sinh viên hoặc email đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                LoadRegisterSelectLists(model.MaLop);
                return View(model);
            }

            var user = new NguoiDung
            {
                MaNguoiDung = CreateUserId(),
                MaSinhVien = model.MaSinhVien,
                HoTen = model.HoTen,
                Email = model.Email,
                NgaySinh = model.NgaySinh ?? DateTime.Today,
                MaLop = model.MaLop,
                MaQuyen = FindStudentRoleId(),
                MatKhau = HashPassword(model.MatKhau)
            };

            db.NguoiDungs.Add(user);
            db.SaveChanges();

            FormsAuthentication.SetAuthCookie(user.MaNguoiDung, false);
            SaveUserSession(user);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Hiển thị trang thông tin cá nhân của người dùng hiện tại.
        /// </summary>
        /// <returns>View thông tin cá nhân.</returns>
        [Authorize]
        public new ActionResult Profile()
        {
            var user = FindCurrentUser();
            if (user == null)
            {
                return HttpNotFound();
            }

            LoadRegisterSelectLists(user.MaLop);
            return View(new ProfileViewModel
            {
                MaSinhVien = user.MaSinhVien,
                HoTen = user.HoTen,
                Email = user.Email,
                NgaySinh = user.NgaySinh ?? DateTime.Today,
                MaLop = user.MaLop
            });
        }

        /// <summary>
        /// Cập nhật thông tin cá nhân của người dùng hiện tại.
        /// </summary>
        /// <param name="model">Dữ liệu thông tin cá nhân mới.</param>
        /// <returns>Chuyển hướng lại trang Profile sau khi cập nhật thành công.</returns>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public new ActionResult Profile(ProfileViewModel model)
        {
            var user = FindCurrentUser();
            if (user == null)
            {
                return HttpNotFound();
            }

            if (db.NguoiDungs.Any(x => x.MaNguoiDung != user.MaNguoiDung && x.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được dùng cho tài khoản khác.");
            }

            if (!ModelState.IsValid)
            {
                LoadRegisterSelectLists(model.MaLop);
                return View(model);
            }

            user.HoTen = model.HoTen;
            user.Email = model.Email;
            user.NgaySinh = model.NgaySinh ?? DateTime.Today;
            user.MaLop = model.MaLop;

            if (!string.IsNullOrWhiteSpace(model.MatKhauMoi))
            {
                user.MatKhau = HashPassword(model.MatKhauMoi);
            }

            db.SaveChanges();
            SaveUserSession(user);
            TempData["Message"] = "Đã cập nhật thông tin tài khoản.";
            return RedirectToAction("Profile");
        }

        /// <summary>
        /// Thực hiện đăng xuất người dùng.
        /// </summary>
        /// <returns>Chuyển hướng về trang đăng nhập.</returns>
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        /// <summary>
        /// Lưu thông tin người dùng vào Session.
        /// </summary>
        /// <param name="user">Đối tượng người dùng cần lưu.</param>
        private void SaveUserSession(NguoiDung user)
        {
            Session["MaNguoiDung"] = user.MaNguoiDung;
            Session["HoTen"] = user.HoTen;
            Session["MaSinhVien"] = user.MaSinhVien;
            Session["MaQuyen"] = user.MaQuyen;
            Session["TenQuyen"] = user.QuyenHan != null ? user.QuyenHan.TenQuyen : "";
            Session["RoleKey"] = RoleHelper.Normalize(user.MaQuyen, user.QuyenHan != null ? user.QuyenHan.TenQuyen : "");
        }

        /// <summary>
        /// Tìm kiếm người dùng hiện đang đăng nhập.
        /// </summary>
        /// <returns>Đối tượng NguoiDung nếu tìm thấy, ngược lại trả về null.</returns>
        private NguoiDung FindCurrentUser()
        {
            var userId = (Session["MaNguoiDung"] ?? User.Identity.Name ?? "").ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return db.NguoiDungs.Include("QuyenHan").FirstOrDefault(x => x.MaNguoiDung == userId);
        }

        /// <summary>
        /// Tải danh sách lớp học cho các DropdownList.
        /// </summary>
        /// <param name="selectedClassId">Mã lớp học đang được chọn.</param>
        private void LoadRegisterSelectLists(string selectedClassId = null)
        {
            ViewBag.MaLop = new SelectList(db.LopHocs.OrderBy(x => x.TenLop), "MaLop", "TenLop", selectedClassId);
        }

        /// <summary>
        /// Tìm mã quyền hạn của Sinh viên.
        /// </summary>
        /// <returns>Mã quyền của sinh viên.</returns>
        private string FindStudentRoleId()
        {
            var role = db.QuyenHans
                .FirstOrDefault(x => x.TenQuyen.Contains("Sinh viên") || x.TenQuyen.Contains("sinh viên") || x.MaQuyen == "SV");

            if (role != null)
            {
                return role.MaQuyen;
            }

            return db.QuyenHans.OrderBy(x => x.MaQuyen).Select(x => x.MaQuyen).FirstOrDefault();
        }

        /// <summary>
        /// Tạo mới mã người dùng (User ID) duy nhất.
        /// </summary>
        /// <returns>Mã người dùng mới.</returns>
        private string CreateUserId()
        {
            var prefix = "ND";
            var next = db.NguoiDungs.Count() + 1;
            string id;

            do
            {
                id = prefix + next.ToString("0000");
                next++;
            }
            while (db.NguoiDungs.Any(x => x.MaNguoiDung == id));

            return id;
        }

        /// <summary>
        /// Mã hóa mật khẩu sử dụng thuật toán SHA256.
        /// </summary>
        /// <param name="password">Mật khẩu cần mã hóa.</param>
        /// <returns>Chuỗi mật khẩu đã mã hóa.</returns>
        private static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Mã hóa mật khẩu sử dụng thuật toán MD5 (dành cho các mật khẩu cũ).
        /// </summary>
        /// <param name="password">Mật khẩu cần mã hóa.</param>
        /// <returns>Chuỗi mật khẩu đã mã hóa MD5.</returns>
        private static string HashLegacyMd5(string password)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Giải phóng các tài nguyên không sử dụng.
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

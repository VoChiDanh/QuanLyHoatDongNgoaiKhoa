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
using ClosedXML.Excel;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace QL_HDNK.Controllers
{
    [RoleAuthorize(RoleKeys.Admin)]
    public class NguoiDungsController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách người dùng.
        /// </summary>
        /// <returns>View danh sách người dùng.</returns>
        public ActionResult Index()
        {
            var nguoiDungs = db.NguoiDungs.Include(n => n.LopHoc).Include(n => n.QuyenHan);
            return View(nguoiDungs.ToList());
        }

        /// <summary>
        /// Hiển thị trang nhập dữ liệu từ Excel.
        /// </summary>
        /// <returns>View nhập Excel.</returns>
        public ActionResult ImportExcel()
        {
            return View();
        }

        /// <summary>
        /// Xử lý file Excel được tải lên để đồng bộ dữ liệu sinh viên.
        /// </summary>
        /// <param name="file">File Excel được chọn.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công, hoặc hiển thị lỗi.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportExcel(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn file Excel.");
                return View();
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                ModelState.AddModelError("", "Chỉ hỗ trợ file Excel (.xlsx, .xls).");
                return View();
            }

            try
            {
                using (var workbook = new XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua tiêu đề

                    int count = 0;
                    string studentRoleId = FindStudentRoleId();
                    
                    // Lấy mã số tiếp theo một lần để dùng trong vòng lặp
                    int nextIdNumber = GetNextUserIdNumber();

                    foreach (var row in rows)
                    {
                        // Bỏ qua cột 1 (STT), dữ liệu bắt đầu từ cột 2
                        var c2 = row.Cell(2).Value; // MaSV
                        var c3 = row.Cell(3).Value; // HoTen
                        var c4 = row.Cell(4).Value; // NgaySinh hoặc Email
                        var c5 = row.Cell(5).Value; // Email hoặc NgaySinh
                        var c6 = row.Cell(6).Value; // MaLop

                        string maSV = c2.IsBlank ? "" : c2.ToString().Trim();
                        if (string.IsNullOrEmpty(maSV)) continue;

                        string hoTen = c3.IsBlank ? null : c3.ToString().Trim();
                        
                        DateTime? ngaySinh = null;
                        string email = null;

                        // Tự động nhận diện cột Ngày sinh và Email (thường bị hoán đổi ở cột 4 và 5)
                        string s4 = c4.IsBlank ? "" : c4.ToString().Trim();
                        string s5 = c5.IsBlank ? "" : c5.ToString().Trim();

                        if (s4.Contains("@") || (!s5.Contains("@") && !c4.IsDateTime && c5.IsDateTime))
                        {
                            // Cột 4 có vẻ là Email, Cột 5 là Ngày sinh
                            email = s4;
                            if (c5.IsDateTime) ngaySinh = c5.GetDateTime();
                            else if (!string.IsNullOrEmpty(s5) && DateTime.TryParse(s5, out DateTime dt)) ngaySinh = dt;
                        }
                        else
                        {
                            // Cột 4 có vẻ là Ngày sinh, Cột 5 là Email
                            email = s5;
                            if (c4.IsDateTime) ngaySinh = c4.GetDateTime();
                            else if (!string.IsNullOrEmpty(s4) && DateTime.TryParse(s4, out DateTime dt)) ngaySinh = dt;
                        }

                        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                        {
                            email = null;
                        }

                        string maLop = c6.IsBlank ? null : c6.ToString().Trim();

                        // Kiểm tra mã lớp có tồn tại không
                        if (!string.IsNullOrEmpty(maLop) && !db.LopHocs.Any(l => l.MaLop == maLop))
                        {
                            maLop = null; 
                        }

                        var user = db.NguoiDungs.FirstOrDefault(x => x.MaSinhVien == maSV);
                        if (user != null)
                        {
                            // Cập nhật
                            user.HoTen = hoTen ?? user.HoTen;
                            user.NgaySinh = ngaySinh ?? user.NgaySinh;
                            user.Email = email ?? user.Email;
                            user.MaLop = maLop ?? user.MaLop;
                        }
                        else
                        {
                            // Thêm mới
                            string newId;
                            do {
                                newId = "ND" + nextIdNumber.ToString("0000");
                                nextIdNumber++;
                            } while (db.NguoiDungs.Any(x => x.MaNguoiDung == newId));

                            user = new NguoiDung
                            {
                                MaNguoiDung = newId,
                                MaSinhVien = maSV,
                                HoTen = hoTen,
                                NgaySinh = ngaySinh,
                                Email = email,
                                MaLop = maLop,
                                MaQuyen = studentRoleId,
                                MatKhau = HashPassword(maSV)
                            };
                            db.NguoiDungs.Add(user);
                        }
                        count++;
                    }
                    db.SaveChanges();
                    TempData["Message"] = $"Đã đồng bộ {count} sinh viên từ file Excel.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        message += " | Root: " + ex.InnerException.InnerException.Message;
                    }
                }
                ModelState.AddModelError("", "Lỗi xử lý file: " + message);
                return View();
            }
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
        /// Lấy mã số tiếp theo cho mã người dùng.
        /// </summary>
        /// <returns>Số thứ tự tiếp theo.</returns>
        private int GetNextUserIdNumber()
        {
            var prefix = "ND";
            var maxId = db.NguoiDungs
                .Where(x => x.MaNguoiDung.StartsWith(prefix))
                .Select(x => x.MaNguoiDung)
                .ToList()
                .Select(id => {
                    int num;
                    return int.TryParse(id.Substring(prefix.Length), out num) ? num : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return maxId + 1;
        }

        /// <summary>
        /// Tạo mã người dùng mới.
        /// </summary>
        /// <returns>Mã người dùng mới chuỗi NDxxxx.</returns>
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
        /// Mã hóa mật khẩu người dùng.
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
        /// Hiển thị chi tiết một người dùng.
        /// </summary>
        /// <param name="id">Mã người dùng cần xem.</param>
        /// <returns>View chi tiết hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }
            return View(nguoiDung);
        }

        /// <summary>
        /// Hiển thị trang tạo mới người dùng.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            ViewBag.MaLop = new SelectList(db.LopHocs, "MaLop", "TenLop");
            ViewBag.MaQuyen = new SelectList(db.QuyenHans, "MaQuyen", "TenQuyen");
            return View(new NguoiDung { NgaySinh = DateTime.Today });
        }

        /// <summary>
        /// Xử lý tạo mới người dùng và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="nguoiDung">Đối tượng người dùng cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaNguoiDung,MaSinhVien,MatKhau,HoTen,NgaySinh,Email,MaLop,MaQuyen")] NguoiDung nguoiDung)
        {
            nguoiDung.NgaySinh = nguoiDung.NgaySinh ?? DateTime.Today;

            if (ModelState.IsValid)
            {
                db.NguoiDungs.Add(nguoiDung);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaLop = new SelectList(db.LopHocs, "MaLop", "TenLop", nguoiDung.MaLop);
            ViewBag.MaQuyen = new SelectList(db.QuyenHans, "MaQuyen", "TenQuyen", nguoiDung.MaQuyen);
            return View(nguoiDung);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa người dùng.
        /// </summary>
        /// <param name="id">Mã người dùng cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }

            if (!nguoiDung.NgaySinh.HasValue)
            {
                nguoiDung.NgaySinh = DateTime.Today;
            }

            ViewBag.MaLop = new SelectList(db.LopHocs, "MaLop", "TenLop", nguoiDung.MaLop);
            ViewBag.MaQuyen = new SelectList(db.QuyenHans, "MaQuyen", "TenQuyen", nguoiDung.MaQuyen);
            return View(nguoiDung);
        }

        /// <summary>
        /// Cập nhật thông tin người dùng vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="nguoiDung">Đối tượng người dùng đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaNguoiDung,MaSinhVien,MatKhau,HoTen,NgaySinh,Email,MaLop,MaQuyen")] NguoiDung nguoiDung)
        {
            nguoiDung.NgaySinh = nguoiDung.NgaySinh ?? DateTime.Today;

            if (ModelState.IsValid)
            {
                db.Entry(nguoiDung).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaLop = new SelectList(db.LopHocs, "MaLop", "TenLop", nguoiDung.MaLop);
            ViewBag.MaQuyen = new SelectList(db.QuyenHans, "MaQuyen", "TenQuyen", nguoiDung.MaQuyen);
            return View(nguoiDung);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa người dùng.
        /// </summary>
        /// <param name="id">Mã người dùng cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }
            return View(nguoiDung);
        }

        /// <summary>
        /// Thực hiện xóa người dùng khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã người dùng cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            db.NguoiDungs.Remove(nguoiDung);
            db.SaveChanges();
            return RedirectToAction("Index");
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



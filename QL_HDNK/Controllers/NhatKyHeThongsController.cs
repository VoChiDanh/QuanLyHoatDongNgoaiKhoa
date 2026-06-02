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

namespace QL_HDNK.Controllers
{
    [RoleAuthorize(RoleKeys.Admin)]
    public class NhatKyHeThongsController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách nhật ký hệ thống với các bộ lọc tìm kiếm.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm trong chi tiết nhật ký.</param>
        /// <param name="maNguoiDung">Lọc theo người dùng thực hiện.</param>
        /// <param name="fromDate">Lọc từ ngày.</param>
        /// <param name="toDate">Lọc đến ngày.</param>
        /// <returns>View danh sách nhật ký.</returns>
        public ActionResult Index(string search, string maNguoiDung, DateTime? fromDate, DateTime? toDate)
        {
            var query = db.NhatKyHeThongs.Include(n => n.NguoiDung).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(n => n.ChiTiet.Contains(search));
            }

            if (!string.IsNullOrEmpty(maNguoiDung))
            {
                query = query.Where(n => n.MaNguoiDung == maNguoiDung);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(n => n.ThoiGian >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(n => n.ThoiGian <= endOfDay);
            }

            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs.OrderBy(x => x.HoTen), "MaNguoiDung", "HoTen", maNguoiDung);
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var logs = query.OrderByDescending(n => n.ThoiGian).Take(500).ToList();
            
            // Xử lý thông tin người được sửa / đối tượng tác động
            var targetDict = new Dictionary<string, string>();
            var allUsers = db.NguoiDungs.ToList();
            var allDangKy = db.DangKy_DiemDanh.ToList();
            
            foreach (var log in logs)
            {
                if (string.IsNullOrEmpty(log.ChiTiet)) continue;
                
                string targetInfo = "";
                
                // Trích xuất MaNguoiDung nếu có trong ChiTiet (ví dụ: NguoiDung [MaNguoiDung=USR001])
                if (log.ChiTiet.Contains("NguoiDung [MaNguoiDung="))
                {
                    int start = log.ChiTiet.IndexOf("MaNguoiDung=") + 12;
                    int end = log.ChiTiet.IndexOf("]", start);
                    if (end > start)
                    {
                        string maNguoiDungSua = log.ChiTiet.Substring(start, end - start);
                        var u = allUsers.FirstOrDefault(x => x.MaNguoiDung == maNguoiDungSua);
                        if (u != null)
                        {
                            targetInfo = u.HoTen + " (" + u.MaSinhVien + ")";
                        }
                        else
                        {
                            targetInfo = maNguoiDungSua;
                        }
                    }
                }
                // Trích xuất MaDangKy nếu là DangKy_DiemDanh
                else if (log.ChiTiet.Contains("DangKy_DiemDanh [MaDangKy="))
                {
                    int start = log.ChiTiet.IndexOf("MaDangKy=") + 9;
                    int end = log.ChiTiet.IndexOf("]", start);
                    if (end > start)
                    {
                        string maDangKy = log.ChiTiet.Substring(start, end - start);
                        var dk = allDangKy.FirstOrDefault(x => x.MaDangKy == maDangKy);
                        if (dk != null)
                        {
                            var u = allUsers.FirstOrDefault(x => x.MaNguoiDung == dk.MaNguoiDung);
                            if (u != null)
                            {
                                targetInfo = u.HoTen + " (" + u.MaSinhVien + ")";
                            }
                            else
                            {
                                targetInfo = maDangKy;
                            }
                        }
                        else
                        {
                            targetInfo = maDangKy;
                        }
                    }
                }
                
                targetDict[log.MaNhatKy] = targetInfo;
            }
            
            ViewBag.TargetDict = targetDict;

            return View(logs);
        }

        /// <summary>
        /// Hiển thị chi tiết một dòng nhật ký hệ thống.
        /// </summary>
        /// <param name="id">Mã nhật ký cần xem.</param>
        /// <returns>View chi tiết nhật ký hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NhatKyHeThong nhatKyHeThong = db.NhatKyHeThongs.Find(id);
            if (nhatKyHeThong == null)
            {
                return HttpNotFound();
            }
            return View(nhatKyHeThong);
        }

        /// <summary>
        /// Hiển thị trang tạo mới nhật ký.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien");
            return View(new NhatKyHeThong { ThoiGian = DateTime.Now });
        }

        /// <summary>
        /// Xử lý lưu nhật ký mới vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="nhatKyHeThong">Đối tượng nhật ký cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaNhatKy,MaNguoiDung,ChiTiet,ThoiGian")] NhatKyHeThong nhatKyHeThong)
        {
            if (nhatKyHeThong.ThoiGian == DateTime.MinValue)
            {
                nhatKyHeThong.ThoiGian = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                db.NhatKyHeThongs.Add(nhatKyHeThong);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", nhatKyHeThong.MaNguoiDung);
            return View(nhatKyHeThong);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa nhật ký.
        /// </summary>
        /// <param name="id">Mã nhật ký cần sửa.</param>
        /// <returns>View chỉnh sửa nhật ký hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NhatKyHeThong nhatKyHeThong = db.NhatKyHeThongs.Find(id);
            if (nhatKyHeThong == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", nhatKyHeThong.MaNguoiDung);
            return View(nhatKyHeThong);
        }

        /// <summary>
        /// Cập nhật thông tin nhật ký vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="nhatKyHeThong">Đối tượng nhật ký đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaNhatKy,MaNguoiDung,ChiTiet,ThoiGian")] NhatKyHeThong nhatKyHeThong)
        {
            if (ModelState.IsValid)
            {
                db.Entry(nhatKyHeThong).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "MaSinhVien", nhatKyHeThong.MaNguoiDung);
            return View(nhatKyHeThong);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa nhật ký.
        /// </summary>
        /// <param name="id">Mã nhật ký cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NhatKyHeThong nhatKyHeThong = db.NhatKyHeThongs.Find(id);
            if (nhatKyHeThong == null)
            {
                return HttpNotFound();
            }
            return View(nhatKyHeThong);
        }

        /// <summary>
        /// Thực hiện xóa nhật ký khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã nhật ký cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa thành công.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            NhatKyHeThong nhatKyHeThong = db.NhatKyHeThongs.Find(id);
            db.NhatKyHeThongs.Remove(nhatKyHeThong);
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



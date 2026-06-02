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
    public class LopHocsController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các lớp học.
        /// </summary>
        /// <returns>View danh sách lớp học.</returns>
        public ActionResult Index()
        {
            var lopHocs = db.LopHocs.Include(l => l.Khoa);
            return View(lopHocs.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một lớp học.
        /// </summary>
        /// <param name="id">Mã lớp học cần xem chi tiết.</param>
        /// <returns>View chi tiết lớp học hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHoc lopHoc = db.LopHocs.Find(id);
            if (lopHoc == null)
            {
                return HttpNotFound();
            }
            return View(lopHoc);
        }

        /// <summary>
        /// Hiển thị trang tạo mới lớp học.
        /// </summary>
        /// <returns>View tạo mới lớp học.</returns>
        public ActionResult Create()
        {
            ViewBag.MaKhoa = new SelectList(db.Khoas, "MaKhoa", "TenKhoa");
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới lớp học và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="lopHoc">Đối tượng lớp học cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaLop,TenLop,MaKhoa,KhoaHoc")] LopHoc lopHoc)
        {
            if (ModelState.IsValid)
            {
                db.LopHocs.Add(lopHoc);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaKhoa = new SelectList(db.Khoas, "MaKhoa", "TenKhoa", lopHoc.MaKhoa);
            return View(lopHoc);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa lớp học.
        /// </summary>
        /// <param name="id">Mã lớp học cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa lớp học hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHoc lopHoc = db.LopHocs.Find(id);
            if (lopHoc == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaKhoa = new SelectList(db.Khoas, "MaKhoa", "TenKhoa", lopHoc.MaKhoa);
            return View(lopHoc);
        }

        /// <summary>
        /// Cập nhật thông tin lớp học vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="lopHoc">Đối tượng lớp học đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaLop,TenLop,MaKhoa,KhoaHoc")] LopHoc lopHoc)
        {
            if (ModelState.IsValid)
            {
                db.Entry(lopHoc).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaKhoa = new SelectList(db.Khoas, "MaKhoa", "TenKhoa", lopHoc.MaKhoa);
            return View(lopHoc);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa lớp học.
        /// </summary>
        /// <param name="id">Mã lớp học cần xóa.</param>
        /// <returns>View xóa lớp học hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LopHoc lopHoc = db.LopHocs.Find(id);
            if (lopHoc == null)
            {
                return HttpNotFound();
            }
            return View(lopHoc);
        }

        /// <summary>
        /// Thực hiện xóa lớp học khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã lớp học cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            LopHoc lopHoc = db.LopHocs.Find(id);
            db.LopHocs.Remove(lopHoc);
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



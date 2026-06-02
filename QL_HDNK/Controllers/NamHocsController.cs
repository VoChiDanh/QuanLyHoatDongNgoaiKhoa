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
    public class NamHocsController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các năm học.
        /// </summary>
        /// <returns>View danh sách năm học.</returns>
        public ActionResult Index()
        {
            return View(db.NamHocs.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một năm học.
        /// </summary>
        /// <param name="id">Mã năm học cần xem chi tiết.</param>
        /// <returns>View chi tiết hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NamHoc namHoc = db.NamHocs.Find(id);
            if (namHoc == null)
            {
                return HttpNotFound();
            }
            return View(namHoc);
        }

        /// <summary>
        /// Hiển thị trang tạo mới năm học.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới năm học và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="namHoc">Đối tượng năm học cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaNamHoc,TenNamHoc,TuNgay,DenNgay")] NamHoc namHoc)
        {
            if (ModelState.IsValid)
            {
                db.NamHocs.Add(namHoc);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(namHoc);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa năm học.
        /// </summary>
        /// <param name="id">Mã năm học cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NamHoc namHoc = db.NamHocs.Find(id);
            if (namHoc == null)
            {
                return HttpNotFound();
            }
            return View(namHoc);
        }

        /// <summary>
        /// Cập nhật thông tin năm học vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="namHoc">Đối tượng năm học đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaNamHoc,TenNamHoc,TuNgay,DenNgay")] NamHoc namHoc)
        {
            if (ModelState.IsValid)
            {
                db.Entry(namHoc).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(namHoc);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa năm học.
        /// </summary>
        /// <param name="id">Mã năm học cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NamHoc namHoc = db.NamHocs.Find(id);
            if (namHoc == null)
            {
                return HttpNotFound();
            }
            return View(namHoc);
        }

        /// <summary>
        /// Thực hiện xóa năm học khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã năm học cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            NamHoc namHoc = db.NamHocs.Find(id);
            db.NamHocs.Remove(namHoc);
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



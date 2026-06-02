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
    public class KhoasController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các khoa.
        /// </summary>
        /// <returns>View danh sách khoa.</returns>
        public ActionResult Index()
        {
            return View(db.Khoas.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một khoa.
        /// </summary>
        /// <param name="id">Mã khoa cần xem chi tiết.</param>
        /// <returns>View chi tiết khoa hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Khoa khoa = db.Khoas.Find(id);
            if (khoa == null)
            {
                return HttpNotFound();
            }
            return View(khoa);
        }

        /// <summary>
        /// Hiển thị trang tạo mới khoa.
        /// </summary>
        /// <returns>View tạo mới khoa.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới khoa và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="khoa">Đối tượng khoa cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaKhoa,TenKhoa")] Khoa khoa)
        {
            if (ModelState.IsValid)
            {
                db.Khoas.Add(khoa);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(khoa);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa khoa.
        /// </summary>
        /// <param name="id">Mã khoa cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa khoa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Khoa khoa = db.Khoas.Find(id);
            if (khoa == null)
            {
                return HttpNotFound();
            }
            return View(khoa);
        }

        /// <summary>
        /// Cập nhật thông tin khoa vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="khoa">Đối tượng khoa đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaKhoa,TenKhoa")] Khoa khoa)
        {
            if (ModelState.IsValid)
            {
                db.Entry(khoa).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(khoa);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa khoa.
        /// </summary>
        /// <param name="id">Mã khoa cần xóa.</param>
        /// <returns>View xóa khoa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Khoa khoa = db.Khoas.Find(id);
            if (khoa == null)
            {
                return HttpNotFound();
            }
            return View(khoa);
        }

        /// <summary>
        /// Thực hiện xóa khoa khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã khoa cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Khoa khoa = db.Khoas.Find(id);
            db.Khoas.Remove(khoa);
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



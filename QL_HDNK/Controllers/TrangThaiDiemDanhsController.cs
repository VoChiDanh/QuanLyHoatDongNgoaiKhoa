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
    public class TrangThaiDiemDanhsController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các trạng thái điểm danh.
        /// </summary>
        /// <returns>View danh sách trạng thái điểm danh.</returns>
        public ActionResult Index()
        {
            return View(db.TrangThaiDiemDanhs.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một trạng thái điểm danh.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xem.</param>
        /// <returns>View chi tiết hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiDiemDanh trangThaiDiemDanh = db.TrangThaiDiemDanhs.Find(id);
            if (trangThaiDiemDanh == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiDiemDanh);
        }

        /// <summary>
        /// Hiển thị trang tạo mới trạng thái điểm danh.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới trạng thái điểm danh và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="trangThaiDiemDanh">Đối tượng trạng thái cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaTTDD,TenTTDD")] TrangThaiDiemDanh trangThaiDiemDanh)
        {
            if (ModelState.IsValid)
            {
                db.TrangThaiDiemDanhs.Add(trangThaiDiemDanh);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(trangThaiDiemDanh);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa trạng thái điểm danh.
        /// </summary>
        /// <param name="id">Mã trạng thái cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiDiemDanh trangThaiDiemDanh = db.TrangThaiDiemDanhs.Find(id);
            if (trangThaiDiemDanh == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiDiemDanh);
        }

        /// <summary>
        /// Cập nhật thông tin trạng thái điểm danh vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="trangThaiDiemDanh">Đối tượng trạng thái đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaTTDD,TenTTDD")] TrangThaiDiemDanh trangThaiDiemDanh)
        {
            if (ModelState.IsValid)
            {
                db.Entry(trangThaiDiemDanh).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(trangThaiDiemDanh);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa trạng thái điểm danh.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiDiemDanh trangThaiDiemDanh = db.TrangThaiDiemDanhs.Find(id);
            if (trangThaiDiemDanh == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiDiemDanh);
        }

        /// <summary>
        /// Thực hiện xóa trạng thái điểm danh khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa thành công.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            TrangThaiDiemDanh trangThaiDiemDanh = db.TrangThaiDiemDanhs.Find(id);
            db.TrangThaiDiemDanhs.Remove(trangThaiDiemDanh);
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



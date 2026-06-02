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
    public class TrangThaiDangKiesController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các trạng thái đăng ký.
        /// </summary>
        /// <returns>View danh sách trạng thái đăng ký.</returns>
        public ActionResult Index()
        {
            return View(db.TrangThaiDangKies.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một trạng thái đăng ký.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xem.</param>
        /// <returns>View chi tiết hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiDangKy trangThaiDangKy = db.TrangThaiDangKies.Find(id);
            if (trangThaiDangKy == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiDangKy);
        }

        /// <summary>
        /// Hiển thị trang tạo mới trạng thái đăng ký.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới trạng thái đăng ký và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="trangThaiDangKy">Đối tượng trạng thái cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaTTDK,TenTTDK")] TrangThaiDangKy trangThaiDangKy)
        {
            if (ModelState.IsValid)
            {
                db.TrangThaiDangKies.Add(trangThaiDangKy);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(trangThaiDangKy);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa trạng thái đăng ký.
        /// </summary>
        /// <param name="id">Mã trạng thái cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiDangKy trangThaiDangKy = db.TrangThaiDangKies.Find(id);
            if (trangThaiDangKy == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiDangKy);
        }

        /// <summary>
        /// Cập nhật thông tin trạng thái đăng ký vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="trangThaiDangKy">Đối tượng trạng thái đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaTTDK,TenTTDK")] TrangThaiDangKy trangThaiDangKy)
        {
            if (ModelState.IsValid)
            {
                db.Entry(trangThaiDangKy).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(trangThaiDangKy);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa trạng thái đăng ký.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiDangKy trangThaiDangKy = db.TrangThaiDangKies.Find(id);
            if (trangThaiDangKy == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiDangKy);
        }

        /// <summary>
        /// Thực hiện xóa trạng thái đăng ký khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa thành công.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            TrangThaiDangKy trangThaiDangKy = db.TrangThaiDangKies.Find(id);
            db.TrangThaiDangKies.Remove(trangThaiDangKy);
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



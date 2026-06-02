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
    public class ThamSoQuyDinhsController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các tham số quy định.
        /// </summary>
        /// <returns>View danh sách tham số quy định.</returns>
        public ActionResult Index()
        {
            return View(db.ThamSoQuyDinhs.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một tham số quy định.
        /// </summary>
        /// <param name="id">Mã tham số cần xem.</param>
        /// <returns>View chi tiết hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ThamSoQuyDinh thamSoQuyDinh = db.ThamSoQuyDinhs.Find(id);
            if (thamSoQuyDinh == null)
            {
                return HttpNotFound();
            }
            return View(thamSoQuyDinh);
        }

        /// <summary>
        /// Hiển thị trang tạo mới tham số quy định.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới tham số quy định và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="thamSoQuyDinh">Đối tượng tham số cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaTS,TenTS,GiaTri,DonViTinh,TinhTrang")] ThamSoQuyDinh thamSoQuyDinh)
        {
            if (ModelState.IsValid)
            {
                db.ThamSoQuyDinhs.Add(thamSoQuyDinh);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(thamSoQuyDinh);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa tham số quy định.
        /// </summary>
        /// <param name="id">Mã tham số cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ThamSoQuyDinh thamSoQuyDinh = db.ThamSoQuyDinhs.Find(id);
            if (thamSoQuyDinh == null)
            {
                return HttpNotFound();
            }
            return View(thamSoQuyDinh);
        }

        /// <summary>
        /// Cập nhật thông tin tham số quy định vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="thamSoQuyDinh">Đối tượng tham số đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaTS,TenTS,GiaTri,DonViTinh,TinhTrang")] ThamSoQuyDinh thamSoQuyDinh)
        {
            if (ModelState.IsValid)
            {
                db.Entry(thamSoQuyDinh).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(thamSoQuyDinh);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa tham số quy định.
        /// </summary>
        /// <param name="id">Mã tham số cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ThamSoQuyDinh thamSoQuyDinh = db.ThamSoQuyDinhs.Find(id);
            if (thamSoQuyDinh == null)
            {
                return HttpNotFound();
            }
            return View(thamSoQuyDinh);
        }

        /// <summary>
        /// Thực hiện xóa tham số quy định khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã tham số cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa thành công.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            ThamSoQuyDinh thamSoQuyDinh = db.ThamSoQuyDinhs.Find(id);
            db.ThamSoQuyDinhs.Remove(thamSoQuyDinh);
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



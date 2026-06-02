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
    public class QuyenHansController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các quyền hạn.
        /// </summary>
        /// <returns>View danh sách quyền hạn.</returns>
        public ActionResult Index()
        {
            return View(db.QuyenHans.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một quyền hạn.
        /// </summary>
        /// <param name="id">Mã quyền hạn cần xem.</param>
        /// <returns>View chi tiết hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            QuyenHan quyenHan = db.QuyenHans.Find(id);
            if (quyenHan == null)
            {
                return HttpNotFound();
            }
            return View(quyenHan);
        }

        /// <summary>
        /// Hiển thị trang tạo mới quyền hạn.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới quyền hạn và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="quyenHan">Đối tượng quyền hạn cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaQuyen,TenQuyen,MoTa")] QuyenHan quyenHan)
        {
            if (ModelState.IsValid)
            {
                db.QuyenHans.Add(quyenHan);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(quyenHan);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa quyền hạn.
        /// </summary>
        /// <param name="id">Mã quyền hạn cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            QuyenHan quyenHan = db.QuyenHans.Find(id);
            if (quyenHan == null)
            {
                return HttpNotFound();
            }
            return View(quyenHan);
        }

        /// <summary>
        /// Cập nhật thông tin quyền hạn vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="quyenHan">Đối tượng quyền hạn đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaQuyen,TenQuyen,MoTa")] QuyenHan quyenHan)
        {
            if (ModelState.IsValid)
            {
                db.Entry(quyenHan).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(quyenHan);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa quyền hạn.
        /// </summary>
        /// <param name="id">Mã quyền hạn cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            QuyenHan quyenHan = db.QuyenHans.Find(id);
            if (quyenHan == null)
            {
                return HttpNotFound();
            }
            return View(quyenHan);
        }

        /// <summary>
        /// Thực hiện xóa quyền hạn khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã quyền hạn cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa thành công.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            QuyenHan quyenHan = db.QuyenHans.Find(id);
            db.QuyenHans.Remove(quyenHan);
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



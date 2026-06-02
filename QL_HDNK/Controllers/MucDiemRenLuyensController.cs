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
    [RoleAuthorize(RoleKeys.Admin, RoleKeys.FacultyUnion)]
    public class MucDiemRenLuyensController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các mục điểm rèn luyện.
        /// </summary>
        /// <returns>View danh sách mục điểm rèn luyện.</returns>
        public ActionResult Index()
        {
            var mucDiemRenLuyens = db.MucDiemRenLuyens.Include(m => m.DanhMucDiem);
            return View(mucDiemRenLuyens.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một mục điểm rèn luyện.
        /// </summary>
        /// <param name="id">Mã mục điểm rèn luyện cần xem chi tiết.</param>
        /// <returns>View chi tiết hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MucDiemRenLuyen mucDiemRenLuyen = db.MucDiemRenLuyens.Find(id);
            if (mucDiemRenLuyen == null)
            {
                return HttpNotFound();
            }
            return View(mucDiemRenLuyen);
        }

        /// <summary>
        /// Hiển thị trang tạo mới mục điểm rèn luyện.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucDiems, "MaDanhMuc", "TenDanhMuc");
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới mục điểm rèn luyện và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="mucDiemRenLuyen">Đối tượng mục điểm rèn luyện cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaMucDRL,TenMucDRL,Diem,MaDanhMuc")] MucDiemRenLuyen mucDiemRenLuyen)
        {
            if (ModelState.IsValid)
            {
                db.MucDiemRenLuyens.Add(mucDiemRenLuyen);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaDanhMuc = new SelectList(db.DanhMucDiems, "MaDanhMuc", "TenDanhMuc", mucDiemRenLuyen.MaDanhMuc);
            return View(mucDiemRenLuyen);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa mục điểm rèn luyện.
        /// </summary>
        /// <param name="id">Mã mục điểm rèn luyện cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MucDiemRenLuyen mucDiemRenLuyen = db.MucDiemRenLuyens.Find(id);
            if (mucDiemRenLuyen == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucDiems, "MaDanhMuc", "TenDanhMuc", mucDiemRenLuyen.MaDanhMuc);
            return View(mucDiemRenLuyen);
        }

        /// <summary>
        /// Cập nhật thông tin mục điểm rèn luyện vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="mucDiemRenLuyen">Đối tượng mục điểm rèn luyện đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaMucDRL,TenMucDRL,Diem,MaDanhMuc")] MucDiemRenLuyen mucDiemRenLuyen)
        {
            if (ModelState.IsValid)
            {
                db.Entry(mucDiemRenLuyen).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucDiems, "MaDanhMuc", "TenDanhMuc", mucDiemRenLuyen.MaDanhMuc);
            return View(mucDiemRenLuyen);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa mục điểm rèn luyện.
        /// </summary>
        /// <param name="id">Mã mục điểm rèn luyện cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MucDiemRenLuyen mucDiemRenLuyen = db.MucDiemRenLuyens.Find(id);
            if (mucDiemRenLuyen == null)
            {
                return HttpNotFound();
            }
            return View(mucDiemRenLuyen);
        }

        /// <summary>
        /// Thực hiện xóa mục điểm rèn luyện khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã mục điểm rèn luyện cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            MucDiemRenLuyen mucDiemRenLuyen = db.MucDiemRenLuyens.Find(id);
            db.MucDiemRenLuyens.Remove(mucDiemRenLuyen);
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



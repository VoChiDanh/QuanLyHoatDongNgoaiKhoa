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
    public class DanhMucDiemsController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách danh mục điểm.
        /// </summary>
        /// <returns>View danh sách danh mục điểm.</returns>
        public ActionResult Index()
        {
            var danhMucDiems = db.DanhMucDiems.Include(d => d.DanhMucDiem2);
            return View(danhMucDiems.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một danh mục điểm.
        /// </summary>
        /// <param name="id">Mã danh mục cần xem.</param>
        /// <returns>View chi tiết danh mục hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DanhMucDiem danhMucDiem = db.DanhMucDiems.Find(id);
            if (danhMucDiem == null)
            {
                return HttpNotFound();
            }
            return View(danhMucDiem);
        }

        /// <summary>
        /// Hiển thị trang tạo mới danh mục điểm.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            ViewBag.MaDanhMucCha = new SelectList(db.DanhMucDiems, "MaDanhMuc", "TenDanhMuc");
            return View();
        }

        /// <summary>
        /// Xử lý lưu danh mục điểm mới vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="danhMucDiem">Đối tượng danh mục điểm cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công, ngược lại hiển thị lại trang Create.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaDanhMuc,TenDanhMuc,DiemToiDa,MaDanhMucCha,GhiChu")] DanhMucDiem danhMucDiem)
        {
            if (ModelState.IsValid)
            {
                db.DanhMucDiems.Add(danhMucDiem);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaDanhMucCha = new SelectList(db.DanhMucDiems, "MaDanhMuc", "TenDanhMuc", danhMucDiem.MaDanhMucCha);
            return View(danhMucDiem);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa danh mục điểm.
        /// </summary>
        /// <param name="id">Mã danh mục cần sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DanhMucDiem danhMucDiem = db.DanhMucDiems.Find(id);
            if (danhMucDiem == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaDanhMucCha = new SelectList(db.DanhMucDiems, "MaDanhMuc", "TenDanhMuc", danhMucDiem.MaDanhMucCha);
            return View(danhMucDiem);
        }

        /// <summary>
        /// Cập nhật thông tin danh mục điểm vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="danhMucDiem">Đối tượng danh mục điểm đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaDanhMuc,TenDanhMuc,DiemToiDa,MaDanhMucCha,GhiChu")] DanhMucDiem danhMucDiem)
        {
            if (ModelState.IsValid)
            {
                db.Entry(danhMucDiem).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaDanhMucCha = new SelectList(db.DanhMucDiems, "MaDanhMuc", "TenDanhMuc", danhMucDiem.MaDanhMucCha);
            return View(danhMucDiem);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa danh mục điểm.
        /// </summary>
        /// <param name="id">Mã danh mục cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DanhMucDiem danhMucDiem = db.DanhMucDiems.Find(id);
            if (danhMucDiem == null)
            {
                return HttpNotFound();
            }
            return View(danhMucDiem);
        }

        /// <summary>
        /// Thực hiện xóa danh mục điểm khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã danh mục cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa thành công.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            DanhMucDiem danhMucDiem = db.DanhMucDiems.Find(id);
            db.DanhMucDiems.Remove(danhMucDiem);
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



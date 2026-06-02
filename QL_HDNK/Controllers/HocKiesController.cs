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
    public class HocKiesController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các học kỳ.
        /// </summary>
        /// <returns>View danh sách học kỳ.</returns>
        public ActionResult Index()
        {
            return View(db.HocKies.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một học kỳ.
        /// </summary>
        /// <param name="id">Mã học kỳ cần xem chi tiết.</param>
        /// <returns>View chi tiết học kỳ hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            HocKy hocKy = db.HocKies.Find(id);
            if (hocKy == null)
            {
                return HttpNotFound();
            }
            return View(hocKy);
        }

        /// <summary>
        /// Hiển thị trang tạo mới học kỳ.
        /// </summary>
        /// <returns>View tạo mới học kỳ.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới học kỳ và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="hocKy">Đối tượng học kỳ cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaHocKy,TenHocKy,TuNgay,DenNgay")] HocKy hocKy)
        {
            if (ModelState.IsValid)
            {
                db.HocKies.Add(hocKy);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(hocKy);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa học kỳ.
        /// </summary>
        /// <param name="id">Mã học kỳ cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa học kỳ hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            HocKy hocKy = db.HocKies.Find(id);
            if (hocKy == null)
            {
                return HttpNotFound();
            }
            return View(hocKy);
        }

        /// <summary>
        /// Cập nhật thông tin học kỳ vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="hocKy">Đối tượng học kỳ đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaHocKy,TenHocKy,TuNgay,DenNgay")] HocKy hocKy)
        {
            if (ModelState.IsValid)
            {
                db.Entry(hocKy).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(hocKy);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa học kỳ.
        /// </summary>
        /// <param name="id">Mã học kỳ cần xóa.</param>
        /// <returns>View xóa học kỳ hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            HocKy hocKy = db.HocKies.Find(id);
            if (hocKy == null)
            {
                return HttpNotFound();
            }
            return View(hocKy);
        }

        /// <summary>
        /// Thực hiện xóa học kỳ khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã học kỳ cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            HocKy hocKy = db.HocKies.Find(id);
            db.HocKies.Remove(hocKy);
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



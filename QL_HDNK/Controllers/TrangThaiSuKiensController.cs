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
    public class TrangThaiSuKiensController : Controller
    {
        private QuanLy_HDNKEntities db = new QuanLy_HDNKEntities();

        /// <summary>
        /// Hiển thị danh sách các trạng thái sự kiện.
        /// </summary>
        /// <returns>View danh sách trạng thái sự kiện.</returns>
        public ActionResult Index()
        {
            return View(db.TrangThaiSuKiens.ToList());
        }

        /// <summary>
        /// Hiển thị chi tiết một trạng thái sự kiện.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xem.</param>
        /// <returns>View chi tiết hoặc thông báo lỗi.</returns>
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiSuKien trangThaiSuKien = db.TrangThaiSuKiens.Find(id);
            if (trangThaiSuKien == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiSuKien);
        }

        /// <summary>
        /// Hiển thị trang tạo mới trạng thái sự kiện.
        /// </summary>
        /// <returns>View tạo mới.</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Xử lý tạo mới trạng thái sự kiện và lưu vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="trangThaiSuKien">Đối tượng trạng thái cần tạo.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaTTSK,TenTTSK")] TrangThaiSuKien trangThaiSuKien)
        {
            if (ModelState.IsValid)
            {
                db.TrangThaiSuKiens.Add(trangThaiSuKien);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(trangThaiSuKien);
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa trạng thái sự kiện.
        /// </summary>
        /// <param name="id">Mã trạng thái cần chỉnh sửa.</param>
        /// <returns>View chỉnh sửa hoặc thông báo lỗi.</returns>
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiSuKien trangThaiSuKien = db.TrangThaiSuKiens.Find(id);
            if (trangThaiSuKien == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiSuKien);
        }

        /// <summary>
        /// Cập nhật thông tin trạng thái sự kiện vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="trangThaiSuKien">Đối tượng trạng thái đã chỉnh sửa.</param>
        /// <returns>Chuyển hướng về trang Index nếu thành công.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaTTSK,TenTTSK")] TrangThaiSuKien trangThaiSuKien)
        {
            if (ModelState.IsValid)
            {
                db.Entry(trangThaiSuKien).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(trangThaiSuKien);
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa trạng thái sự kiện.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xóa.</param>
        /// <returns>View xóa hoặc thông báo lỗi.</returns>
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TrangThaiSuKien trangThaiSuKien = db.TrangThaiSuKiens.Find(id);
            if (trangThaiSuKien == null)
            {
                return HttpNotFound();
            }
            return View(trangThaiSuKien);
        }

        /// <summary>
        /// Thực hiện xóa trạng thái sự kiện khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="id">Mã trạng thái cần xóa.</param>
        /// <returns>Chuyển hướng về trang Index sau khi xóa thành công.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            TrangThaiSuKien trangThaiSuKien = db.TrangThaiSuKiens.Find(id);
            db.TrangThaiSuKiens.Remove(trangThaiSuKien);
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



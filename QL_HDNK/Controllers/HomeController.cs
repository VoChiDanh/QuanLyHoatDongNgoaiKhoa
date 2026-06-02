using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_HDNK.Filters;

namespace QL_HDNK.Controllers
    {
    public class HomeController : Controller
        {
        /// <summary>
        /// Hiển thị trang chủ của ứng dụng.
        /// Chuyển hướng Sinh viên đến trang sự kiện dành cho sinh viên.
        /// </summary>
        /// <returns>View trang chủ hoặc trang sự kiện của sinh viên.</returns>
        public ActionResult Index()
            {
            var role = (Session["RoleKey"] ?? "").ToString();
            if (role == RoleKeys.Student)
                {
                return RedirectToAction("SuKien", "SinhVien");
                }
            return View();
            }

        /// <summary>
        /// Hiển thị trang giới thiệu ứng dụng.
        /// </summary>
        /// <returns>View About.</returns>
        public ActionResult About()
            {
            ViewBag.Message = "Your application description page.";

            return View();
            }

        /// <summary>
        /// Hiển thị trang liên hệ.
        /// </summary>
        /// <returns>View Contact.</returns>
        public ActionResult Contact()
            {
            ViewBag.Message = "Your contact page.";

            return View();
            }
        }
    }

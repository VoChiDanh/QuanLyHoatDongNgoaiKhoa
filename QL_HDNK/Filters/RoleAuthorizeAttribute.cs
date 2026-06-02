using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_HDNK.Models;

namespace QL_HDNK.Filters
{
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string[] allowedRoles;

        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            this.allowedRoles = allowedRoles ?? new string[0];
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!base.AuthorizeCore(httpContext))
            {
                return false;
            }

            if (allowedRoles.Length == 0)
            {
                return true;
            }

            var roleKey = httpContext.Session != null ? httpContext.Session["RoleKey"] as string : null;
            if (string.IsNullOrWhiteSpace(roleKey))
            {
                roleKey = LoadRoleFromDatabase(httpContext);
            }

            if (!allowedRoles.Any(role => role == roleKey))
            {
                roleKey = LoadRoleFromDatabase(httpContext);
            }

            return allowedRoles.Any(role => role == roleKey);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                base.HandleUnauthorizedRequest(filterContext);
                return;
            }

            filterContext.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/Forbidden.cshtml"
            };
        }

        private string LoadRoleFromDatabase(HttpContextBase httpContext)
        {
            var userId = httpContext.User.Identity.Name;
            using (var db = new QuanLy_HDNKEntities())
            {
                var user = db.NguoiDungs.Include("QuyenHan").FirstOrDefault(x => x.MaNguoiDung == userId);
                if (user == null)
                {
                    return null;
                }

                var roleKey = RoleHelper.Normalize(user.MaQuyen, user.QuyenHan != null ? user.QuyenHan.TenQuyen : "");
                httpContext.Session["RoleKey"] = roleKey;
                httpContext.Session["TenQuyen"] = user.QuyenHan != null ? user.QuyenHan.TenQuyen : "";
                httpContext.Session["HoTen"] = user.HoTen;
                httpContext.Session["MaSinhVien"] = user.MaSinhVien;
                httpContext.Session["MaNguoiDung"] = user.MaNguoiDung;
                return roleKey;
            }
        }
    }
}

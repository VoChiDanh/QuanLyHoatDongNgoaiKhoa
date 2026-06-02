using System.Web;
using System.Web.Mvc;

namespace QL_HDNK
    {
    public class FilterConfig
        {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
            {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new AuthorizeAttribute());
            }
        }
    }

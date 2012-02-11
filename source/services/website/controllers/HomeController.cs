namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Security;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Website.Models.AccessControl;

    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Index()
        {
            MembershipUser mu = Membership.GetUser();
            if (mu != null)
            {
                ViewBag.Message = mu.Email;
            }

            ViewBag.Message += ", Welcome to ASP.NET MVC!";
            return View();
        }

        [Authorize]
        public ActionResult About()
        {
            return View();
        }
    }
}

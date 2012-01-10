using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TaskStoreWeb.Models;
using System.Web.Security;


namespace TaskStoreWeb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "TaskStore";

            // if the user is logged in, redirect to the TaskList page
            if (Request.IsAuthenticated)
            {
                return RedirectToAction("Index", "TL");
            }
            else
            {
                return View();
                //return RedirectToAction("LogOn", "Account");
            }

        }

        [HttpPost]
        public ActionResult Index(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (Membership.ValidateUser(model.UserName, model.Password))
                    {
                        FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                        if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                            && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "TL");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Incorrect user name or password.");
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Apologies! The app is down - please try again later.");
                }

            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Team()
        {
            return View();
        }

        public ActionResult News()
        {
            return View();
        }

        public ActionResult Blog()
        {
            return View();
        }

        public ActionResult Jobs()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        public ActionResult Mobile()
        {
            return View();
        }

        public ActionResult WelcomeWP7()
        {
            return View();
        }
    }
}

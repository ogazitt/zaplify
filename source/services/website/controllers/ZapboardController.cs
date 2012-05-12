namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web;
    using System.Web.Mvc;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Shared.Entities;
    using BuiltSteady.Zaplify.Website.Models;
    using Microsoft.IdentityModel.Protocols.OAuth.Client;

    public class ZapboardController : BaseController
    {

        public ActionResult Home(bool renewFBToken = false)
        {
            UserDataModel model = new UserDataModel(this);
            try
            {   // force access to validate current user
                var userData = model.UserData;
                UserDataModel.CurrentTheme = model.UserTheme;
                model.RenewFBToken = renewFBToken;
            }
            catch
            {
                return RedirectToAction("SignOut", "Account");
            }
            return View(model);
        }

    }
}

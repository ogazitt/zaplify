namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Web;
    using System.Web.Mvc;
    using Microsoft.IdentityModel.Protocols.OAuth.Client;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Shared.Entities;
    using BuiltSteady.Zaplify.Website.Models;

    using Google.Apis.Authentication.OAuth2;
    using DotNetOpenAuth.OAuth2;
    using DotNetOpenAuth.Messaging;
    using Google.Apis.Calendar.v3;
    using Google.Apis.Calendar.v3.Data;
    using BuiltSteady.Zaplify.Website.Models.AccessControl;

    public class DashboardController : BaseController
    {

        public ActionResult Home(bool renewFBToken = false, string consentStatus = null)
        {
            UserDataModel model = new UserDataModel(this);
            try
            {   // force access to validate current user
                var userData = model.UserData;
                UserDataModel.CurrentTheme = model.UserTheme;
                model.RenewFBToken = renewFBToken;
                model.ConsentStatus = consentStatus;
                // TODO: if consent fails, un-Choose the Suggestion
            }
            catch
            {
                return RedirectToAction("SignOut", "Account");
            }
            return View(model);
        }

        public ActionResult Initialize(int id = 0)
        {
            UserDataModel model = new UserDataModel(this);
            try
            {
                if (model.UserData.Folders.Count == 0)
                {   // only create default folders if no folders exist
                    CreateDefaultFolders(model);
                }
            }
            catch (Exception)
            {
                if (id++ < 3)
                {   // retry upto 3 times before giving up
                    return RedirectToAction("Initialize", "Dashboard", new { id = id });
                }
            } 
            return RedirectToAction("Home", "Dashboard");
        }

        void CreateDefaultFolders(UserDataModel model)
        {
            try
            {
                List<Folder> folders = UserConstants.DefaultFolders(this.CurrentUser);
                foreach (var folder in folders)
                {
                    // child items must be added AFTER saving parent items 
                    // EF cannot determine which items are dependent on eachother
                    List<Item> folderItems = new List<Item>();
                    List<Item> childItems = new List<Item>();
                    foreach (var item in folder.Items)
                    {
                        if (item.ParentID == null) { folderItems.Add(item); }
                        else childItems.Add(item);
                    }
                    folder.Items = folderItems;
                    model.StorageContext.Folders.Add(folder);
                    model.StorageContext.SaveChanges();

                    if (childItems.Count > 0)
                    {
                        foreach (var item in childItems)
                        {
                            model.StorageContext.Items.Add(item);
                        }
                        model.StorageContext.SaveChanges();
                    }
                }

                // create an operation corresponding to the new user creation
                var operation = model.StorageContext.CreateOperation(this.CurrentUser, "POST", (int?)HttpStatusCode.Created, this.CurrentUser, null);

                // kick off the New User workflow
                WorkflowHost.WorkflowHost.InvokeWorkflowForOperation(model.StorageContext, null, operation);
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("CreateDefaultFolders failed", ex);
                throw;
            }
        }

    }
}

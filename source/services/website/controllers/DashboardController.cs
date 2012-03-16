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

    public class DashboardController : BaseController
    {

        public ActionResult Home()
        {
            UserDataModel model = new UserDataModel(this);
            try
            {   // force access to validate current user
                var userData = model.UserData;
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

        // redirect for getting facebook consent access token
        // TODO: should define these constants is configuration
        private const string fbAppId = "411772288837103";
        private const string fbSecretKey = "88a4f4d70a8c4060aa15c593a36062ff";
        private const string fbRedirectPath = "dashboard/facebook";

        public ActionResult Facebook(string code)
        {
            string uriTemplate = "https://graph.facebook.com/oauth/access_token?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}";

            var requestUrl = this.HttpContext.Request.Url;
            var redirectUrl = string.Format("{0}://{1}/{2}", requestUrl.Scheme, requestUrl.Authority, fbRedirectPath);
            string encodedRedirect = HttpUtility.UrlEncode(redirectUrl);
            string uri = string.Format(uriTemplate, fbAppId, encodedRedirect, fbSecretKey, code);

            WebRequest req = WebRequest.Create(uri);
            WebResponse resp = req.GetResponse();

            string token = null;
            DateTime expires;

            using (Stream stream = resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string data = reader.ReadToEnd();

                string[] parts = data.Split('&');
                foreach (var s in parts)
                {
                    string[] kv = s.Split('=');
                    if (kv[0].Equals("access_token", StringComparison.Ordinal))
                        token = kv[1];
                    else if (kv[0].Equals("expires"))
                        expires = DateTime.Now.AddSeconds(int.Parse(kv[1]));
                }
            }

            // TODO: encrypt token, store expiration
            UserStorageContext storage = Storage.NewUserContext;
            User user = storage.Users.Include("UserCredentials").Single<User>(u => u.Name == this.CurrentUser.Name);
            user.UserCredentials[0].FBConsentToken = token;
            storage.SaveChanges();

            
            try
            {   // timestamp suggestion
                SuggestionsStorageContext suggestionsContext = Storage.NewSuggestionsContext;
                Suggestion suggestion = suggestionsContext.Suggestions.Single<Suggestion>(s => s.EntityID == this.CurrentUser.ID && s.FieldName == FieldNames.FacebookConsent);
                suggestion.TimeSelected = DateTime.UtcNow;
                suggestionsContext.SaveChanges();
            }
            catch (Exception) { }

            return RedirectToAction("Home", "Dashboard");
        }


        void CreateDefaultFolders(UserDataModel model)
        {
            try
            {
                DateTime now = DateTime.Now;
                FolderUser folderUser;
                Folder folder;
                Item item;

                // create Activities folder
                folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = this.CurrentUser.ID, PermissionID = Permissions.Full };
                folder = new Folder()
                {
                    ID = folderUser.FolderID,
                    SortOrder = 1000,
                    Name = "Activities",
                    UserID = this.CurrentUser.ID,
                    ItemTypeID = SystemItemTypes.Task,
                    Items = new List<Item>(),
                    FolderUsers = new List<FolderUser>() { folderUser }
                };
                model.StorageContext.Folders.Add(folder);

                // create Tasks list
                item = new Item()
                {
                    ID = Guid.NewGuid(),
                    SortOrder = 1000,
                    Name = "Tasks",
                    FolderID = folder.ID,
                    UserID = this.CurrentUser.ID,
                    IsList = true,
                    ItemTypeID = SystemItemTypes.Task,
                    ParentID = null,
                    Created = now,
                    LastModified = now
                };
                model.StorageContext.Items.Add(item);

                // save the list so that we can insert the Learn Zaplify task under the Tasks list
                // and the Learn Zaplify item will be assured to have a valid ParentID in the DB
                model.StorageContext.SaveChanges();

                // create Learn Zaplify task
                item = new Item()
                {
                    ID = Guid.NewGuid(),
                    SortOrder = 2000,
                    Name = "Learn about Zaplify!",
                    FolderID = folder.ID,
                    UserID = this.CurrentUser.ID,
                    IsList = false,
                    ItemTypeID = SystemItemTypes.Task,
                    ParentID = item.ID,
                    FieldValues = new List<FieldValue>(),
                    Created = now,
                    LastModified = now
                };
                model.StorageContext.Items.Add(item);

                item.FieldValues.Add(
                    ConstantsModel.CreateFieldValue(item.ID, SystemItemTypes.Task, FieldNames.DueDate, DateTime.Today.Date.ToString("yyyy/MM/dd")));
                item.FieldValues.Add(
                    ConstantsModel.CreateFieldValue(item.ID, SystemItemTypes.Task, FieldNames.Description, "Tap the browse button below to discover more about Zaplify."));

                // create Lists folder
                folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = this.CurrentUser.ID, PermissionID = Permissions.Full };
                folder = new Folder()
                {
                    ID = folderUser.FolderID,
                    SortOrder = 2000,
                    Name = "Lists",
                    UserID = this.CurrentUser.ID,
                    ItemTypeID = SystemItemTypes.ListItem,
                    Items = new List<Item>(),
                    FolderUsers = new List<FolderUser>() { folderUser }
                };
                model.StorageContext.Folders.Add(folder);

                // create Groceries list
                item = new Item()
                {
                    ID = Guid.NewGuid(),
                    SortOrder = 3000,
                    Name = "Groceries",
                    FolderID = folder.ID,
                    UserID = this.CurrentUser.ID,
                    IsList = true,
                    ItemTypeID = SystemItemTypes.ShoppingItem,
                    ParentID = null,
                    Created = now,
                    LastModified = now
                };
                model.StorageContext.Items.Add(item);

                // create People folder
                folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = this.CurrentUser.ID, PermissionID = Permissions.Full };
                folder = new Folder()
                {
                    ID = folderUser.FolderID,
                    SortOrder = 3000,
                    Name = "People",
                    UserID = this.CurrentUser.ID,
                    ItemTypeID = SystemItemTypes.Contact,
                    Items = new List<Item>(),
                    FolderUsers = new List<FolderUser>() { folderUser }
                };
                model.StorageContext.Folders.Add(folder);

                // create Places folder
                folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = this.CurrentUser.ID, PermissionID = Permissions.Full };
                folder = new Folder()
                {
                    ID = folderUser.FolderID,
                    SortOrder = 4000,
                    Name = "Places",
                    UserID = this.CurrentUser.ID,
                    ItemTypeID = SystemItemTypes.Location,
                    Items = new List<Item>(),
                    FolderUsers = new List<FolderUser>() { folderUser }
                };
                model.StorageContext.Folders.Add(folder);

                model.StorageContext.SaveChanges();


                // TEMPORARY: add "Get Connected" suggestions for User
                SuggestionsStorageContext suggestionsContext = Storage.NewSuggestionsContext;
                Suggestion connectToFacebook = new Suggestion() 
                {
                    ID = Guid.NewGuid(), EntityID = this.CurrentUser.ID, EntityType = typeof(User).Name,
                    State = "Get Connected", DisplayName = "Connect to Facebook", FieldName = FieldNames.FacebookConsent, 
                    WorkflowInstanceID = Guid.NewGuid(), WorkflowType="InitializeUser"
                };
                suggestionsContext.Suggestions.Add(connectToFacebook);
                Suggestion connectToCloudAD = new Suggestion() 
                {
                    ID = Guid.NewGuid(), EntityID = this.CurrentUser.ID, EntityType = typeof(User).Name,
                    State = "Get Connected", DisplayName = "Connect to Cloud Directory", FieldName = FieldNames.CloudADConsent, 
                    WorkflowInstanceID = connectToFacebook.WorkflowInstanceID, WorkflowType = connectToFacebook.WorkflowType
                };
                suggestionsContext.Suggestions.Add(connectToCloudAD);

                suggestionsContext.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("CreateDefaultFolders failed; ex: " + ex.Message);
                throw;
            }
        }

    }
}

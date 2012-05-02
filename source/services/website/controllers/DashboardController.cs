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

    public class DashboardController : BaseController
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

        public ActionResult Facebook(string code)
        {
            const string fbRedirectPath = "dashboard/facebook";
            string uriTemplate = "https://graph.facebook.com/oauth/access_token?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}";

            var requestUrl = this.HttpContext.Request.Url;
            var redirectUrl = string.Format("{0}://{1}/{2}", requestUrl.Scheme, requestUrl.Authority, fbRedirectPath);
            string encodedRedirect = HttpUtility.UrlEncode(redirectUrl);
            string uri = string.Format(uriTemplate, FBAppID, encodedRedirect, FBAppSecret, code);

            WebRequest req = WebRequest.Create(uri);
            WebResponse resp = req.GetResponse();

            string token = null;
            DateTime? expires = null;

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
                        expires = DateTime.UtcNow.AddSeconds(int.Parse(kv[1]));
                }
            }

            User user = null;
            UserStorageContext storage = Storage.NewUserContext;
            try
            {   // store token

                // TODO: encrypt token
                user = storage.Users.Include("UserCredentials").Single<User>(u => u.Name == this.CurrentUser.Name);
                user.UserCredentials[0].FBConsentToken = token;
                user.UserCredentials[0].FBConsentTokenExpiration = expires;
                user.UserCredentials[0].LastModified = DateTime.UtcNow;
                storage.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Failed to add Facebook credential to User", ex);
                // TODO: should probably return some error to the user
                return RedirectToAction("Home", "Dashboard");
            }
            
            try
            {   
                // find the People folder
                Folder peopleFolder = null;
                try
                {
                    peopleFolder = storage.Folders.First(f => f.UserID == this.CurrentUser.ID && f.ItemTypeID == SystemItemTypes.Contact);
                    if (peopleFolder == null)
                    {
                        TraceLog.TraceError("Facebook Action: cannot find People folder");
                        return RedirectToAction("Home", "Dashboard");
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("Facebook Action: cannot find People folder", ex);
                    return RedirectToAction("Home", "Dashboard");
                }

                // get the Connect to Facebook suggestion and make a copy
                SuggestionsStorageContext suggestionsContext = Storage.NewSuggestionsContext;
                Suggestion suggestion = suggestionsContext.Suggestions.Single<Suggestion>(s => s.EntityID == peopleFolder.ID && s.SuggestionType == SuggestionTypes.GetFBConsent);
                Suggestion oldSuggestion = new Suggestion()
                {
                    ID = suggestion.ID,
                    DisplayName = suggestion.DisplayName,
                    EntityID = suggestion.EntityID,
                    EntityType = suggestion.EntityType,
                    SuggestionType = suggestion.SuggestionType,
                    GroupDisplayName = suggestion.GroupDisplayName,
                    ParentID = suggestion.ParentID,
                    ReasonSelected = suggestion.ReasonSelected,
                    SortOrder = suggestion.SortOrder,
                    State = suggestion.State,
                    TimeSelected = suggestion.TimeSelected,
                    Value = suggestion.Value,
                    WorkflowInstanceID = suggestion.WorkflowInstanceID,
                    WorkflowType = suggestion.WorkflowType
                };

                // timestamp suggestion
                suggestion.TimeSelected = DateTime.UtcNow;
                suggestion.ReasonSelected = Reasons.Chosen;
                suggestionsContext.SaveChanges();

                // create an operation corresponding to the new user creation
                var operation = storage.CreateOperation(user, "PUT", (int?)HttpStatusCode.Accepted, suggestion, oldSuggestion);
                if (operation == null)
                {   
                    TraceLog.TraceError("Facebook Action: failed to create operation");
                    return RedirectToAction("Home", "Dashboard");
                }

                // enqueue a message for the Worker that will wake up the Connect to Facebook workflow
                if (HostEnvironment.IsAzure)
                    MessageQueue.EnqueueMessage(operation.ID);
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Facebook Action: Failed to update and timestamp suggestion", ex);
            }

            return RedirectToAction("Home", "Dashboard");
        }

        public ActionResult CloudAD()
        {
            OAuthClient.RedirectToEndUserEndpoint(
                AzureOAuthConfiguration.ProtectedResourceUrl,
                AuthorizationResponseType.Code,
                new Uri(AzureOAuthConfiguration.GetRedirectUrlAfterEndUserConsent(this.HttpContext.Request.Url)),
                CurrentUser.ID.ToString(),
                null);

            return new EmptyResult();
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
                var operation = model.StorageContext.CreateOperation(this.CurrentUser, "POST", (int?) HttpStatusCode.Created, this.CurrentUser, null);

                // enqueue a message for the Worker that will kick off the New User workflow
                if (HostEnvironment.IsAzure) { MessageQueue.EnqueueMessage(operation.ID); }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("CreateDefaultFolders failed", ex);
                throw;
            }
        }
    }
}

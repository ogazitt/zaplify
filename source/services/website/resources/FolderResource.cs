namespace BuiltSteady.Zaplify.Website.Resources
{   
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.Website.Models;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Shared.Entities;

    [ServiceContract]
    [LogMessages]
    public class FolderResource : BaseResource
    {

        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Folder> DeleteFolder(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Folder>(req, code);  // user not authenticated

            // get the Folder from the message body
            Folder clientFolder = ProcessRequestBody(req, typeof(Folder)) as Folder;
 
            // make sure the Folder ID's match
            if (clientFolder.ID != id)
                return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.BadRequest);

            // get the Folder to be deleted
            try
            {
                Folder requestedFolder = this.StorageContext.Folders.
                    Include("FolderUsers").
                    Include("Items.ItemTags").
                    Include("Items.FieldValues").Single<Folder>(g => g.ID == id);

                // if the requested Folder does not belong to the authenticated user, return 403 Forbidden
                if (requestedFolder.UserID != CurrentUser.ID)
                    return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.Forbidden);

                // remove the itemtags associated with each of the items in this folder
                if (requestedFolder.Items != null && requestedFolder.Items.Count > 0)
                {
                    foreach (Item i in requestedFolder.Items)
                    {
                        // delete all the itemtags associated with this item
                        if (i.ItemTags != null && i.ItemTags.Count > 0)
                        {
                            foreach (var tt in i.ItemTags.ToList())
                                this.StorageContext.ItemTags.Remove(tt);
                        }
                        // delete all the fieldvalues associated with this item
                        if (i.FieldValues != null && i.FieldValues.Count > 0)
                        {
                            foreach (var fv in i.FieldValues.ToList())
                                this.StorageContext.FieldValues.Remove(fv);
                        }
                    }
                }

                // remove the folderusers associated with this folder
                foreach (FolderUser fu in requestedFolder.FolderUsers)
                    this.StorageContext.FolderUsers.Remove(fu);

                // remove the current folder 
                this.StorageContext.Folders.Remove(requestedFolder);
                int rows = this.StorageContext.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<Folder>(req, requestedFolder, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // Folder not found - return 404 Not Found
                //return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.NotFound);
                // Folder not found - it may have been deleted by someone else.  Return 200 OK.
                return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.OK);
            }
        }

        [WebGet(UriTemplate = "")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<Folder>> GetFolders(HttpRequestMessage req)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<List<Folder>>(req, code);

            try
            {
                var folders = this.StorageContext.Folders.Include("FolderUsers").
                    Include("Items.ItemTags").
                    Include("Items.FieldValues").
                    Where(f => f.UserID == CurrentUser.ID).ToList();

                var response = new HttpResponseMessageWrapper<List<Folder>>(req, folders, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                return response;
            }
            catch (Exception)
            {
                // folders not found - return 404 Not Found
                return new HttpResponseMessageWrapper<List<Folder>>(req, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Folder> GetFolder(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<Folder>(req, code);

            try
            {
                Folder requestedFolder = this.StorageContext.Folders.Include("FolderUsers").Include("Items.ItemTags").Include("Items.FieldValues").Single<Folder>(f => f.ID == id);

                // if the requested user is not the same as the authenticated user, return 403 Forbidden
                if (requestedFolder.UserID != CurrentUser.ID)
                    return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.Forbidden);
                else
                    return new HttpResponseMessageWrapper<Folder>(req, requestedFolder, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // folder not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.NotFound);
            }
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Folder> InsertFolder(HttpRequestMessage req)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<Folder>(req, code);

            Folder clientFolder = ProcessRequestBody(req, typeof(Folder)) as Folder;

            // check to make sure the userid in the new folder is the same userid for the current user
            if (clientFolder.UserID == null || clientFolder.UserID == Guid.Empty)
                clientFolder.UserID = CurrentUser.ID;
            if (clientFolder.UserID != CurrentUser.ID)
                return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.Forbidden);

            // fill out the ID if it's not set (e.g. from a javascript client)
            if (clientFolder.ItemTypeID == null || clientFolder.ItemTypeID == Guid.Empty)
                clientFolder.ItemTypeID = SystemItemTypes.Task;

            // default ItemTypeID to Task if not set
            if (clientFolder.ID == null || clientFolder.ID == Guid.Empty)
                clientFolder.ID = Guid.NewGuid();

            // this operation isn't meant to do more than just insert the new folder
            // therefore make sure items collection is empty
            if (clientFolder.Items != null)
                clientFolder.Items.Clear();

            // if the current user isn't in the FolderUsers collection, add it now (must have at least one FolderUser)
            bool addFolderUser = false;
            if (clientFolder.FolderUsers == null || clientFolder.FolderUsers.Count < 1)
                addFolderUser = true;
            else
            {
                var folderUsers = clientFolder.FolderUsers.Where<FolderUser>(gu => gu.UserID == clientFolder.UserID);
                if (folderUsers == null || folderUsers.Count() < 1)
                    addFolderUser = true;
            }
            if (addFolderUser)
            {
                FolderUser fu = new FolderUser() 
                { 
                    ID = Guid.NewGuid(), 
                    UserID = clientFolder.UserID, 
                    FolderID = clientFolder.ID, 
                    PermissionID = 3 /* full */ 
                };
                if (clientFolder.FolderUsers == null)
                    clientFolder.FolderUsers = new List<FolderUser>();
                clientFolder.FolderUsers.Add(fu);
            }

            try
            {
                var folder = this.StorageContext.Folders.Add(clientFolder);
                int rows = this.StorageContext.SaveChanges();
                if (folder == null || rows < 1)
                    return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.Conflict);
                else
                    return new HttpResponseMessageWrapper<Folder>(req, folder, HttpStatusCode.Created);
            }
            catch (Exception)
            {
                // check for the condition where the folder is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbFolder = this.StorageContext.Folders.Single(g => g.ID == clientFolder.ID);
                    if (dbFolder.Name == clientFolder.Name &&
                        dbFolder.UserID == clientFolder.UserID)
                        return new HttpResponseMessageWrapper<Folder>(req, dbFolder, HttpStatusCode.Accepted);
                    else
                        return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.Conflict);
                }
                catch (Exception)
                {
                    // folder not inserted - return 409 Conflict
                    return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.Conflict);
                }
            }
        }

        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Folder> UpdateFolder(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Folder>(req, code);  // user not authenticated

            // the body will be two Folders - the original and the new values.  Verify this
            List<Folder> clientFolders = ProcessRequestBody(req, typeof(List<Folder>)) as List<Folder>;
            if (clientFolders.Count != 2)
                return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.BadRequest);

            // get the original and new Folders out of the message body
            Folder originalFolder = clientFolders[0];
            Folder newFolder = clientFolders[1];

            // make sure the Folder ID's match
            if (originalFolder.ID != newFolder.ID)
                return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.BadRequest);
            if (originalFolder.ID != id)
                return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.BadRequest);

            try
            {
                Folder requestedFolder = this.StorageContext.Folders.Single<Folder>(t => t.ID == id);

                // if the Folder does not belong to the authenticated user, return 403 Forbidden
                if (requestedFolder.UserID != CurrentUser.ID)
                    return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.Forbidden);
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalFolder.UserID = requestedFolder.UserID;
                newFolder.UserID = requestedFolder.UserID;

                bool changed = Update(requestedFolder, originalFolder, newFolder);
                if (changed == true)
                {
                    if (this.StorageContext.SaveChanges() < 1)
                        return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<Folder>(req, requestedFolder, HttpStatusCode.Accepted);
                }
                else
                    return new HttpResponseMessageWrapper<Folder>(req, requestedFolder, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // Folder not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Folder>(req, HttpStatusCode.NotFound);
            }
        }


        // Update the requested folder with values from the new folder
        // Currently, the algorithm updates only if the server's current value is equal 
        // to the original value passed in.
        // NOTE: the server value for folders currently does not include the Item collection
        // because we did not .Include() it in the EF query.  This works well so that the update
        // loop bypasses the Items collection - we are only updating scalar values.
        private bool Update(Folder requestedFolder, Folder originalFolder, Folder newFolder)
        {
            bool updated = false;
            // TODO: timestamps!
            Type t = requestedFolder.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedFolder, null);
                object origValue = pi.GetValue(originalFolder, null);
                object newValue = pi.GetValue(newFolder, null);

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, make the update
                    if (Object.Equals(serverValue, origValue))
                    {
                        pi.SetValue(requestedFolder, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}
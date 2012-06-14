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

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Shared.Entities;
    using BuiltSteady.Zaplify.Website.Helpers;

    [ServiceContract]
    [LogMessages]
    public class FolderResource : BaseResource
    {

        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Folder> DeleteFolder(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Folder>(req, operation, code);
            }

            // get the folder from the message body if one was passed
            Folder clientFolder;
            if (req.Content.Headers.ContentLength > 0)
            {
                clientFolder = null;
                code = ProcessRequestBody<Folder>(req, out clientFolder, out operation);
                if (code != HttpStatusCode.OK)  // error encountered processing body
                    return ReturnResult<Folder>(req, operation, code);

                if (clientFolder.ID != id)
                {   // IDs must match
                    TraceLog.TraceError("ID in URL does not match entity body)");
                    return ReturnResult<Folder>(req, operation, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                // otherwise get the client folder from the database
                try
                {
                    clientFolder = this.StorageContext.Folders.Single<Folder>(f => f.ID == id);
                    operation = this.StorageContext.CreateOperation(CurrentUser, req.Method.Method, null, clientFolder, null);
                }
                catch (Exception ex)
                {   // item not found - it may have been deleted by someone else.  Return 200 OK.
                    TraceLog.TraceException("Entity not found; return OK", ex);
                    return ReturnResult<Folder>(req, operation, HttpStatusCode.OK);
                }
            }

            // get the Folder to be deleted
            try
            {
                Folder requestedFolder = this.StorageContext.Folders.
                    Include("FolderUsers").
                    Include("Items.ItemTags").
                    Include("Items.FieldValues").Single<Folder>(g => g.ID == id);

                // if the requested Folder does not belong to the authenticated user, return 403 Forbidden
                if (requestedFolder.UserID != CurrentUser.ID)
                {
                    TraceLog.TraceError("Entity does not belong to current user");
                    return ReturnResult<Folder>(req, operation, HttpStatusCode.Forbidden);
                }

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

                // SMILLET: delete cascade will delete FolderUsers (this throws exception)
                // remove the folderusers associated with this folder
                //foreach (FolderUser fu in requestedFolder.FolderUsers)
                //    this.StorageContext.FolderUsers.Remove(fu);

                try
                {
                    // items inside of folders have referential integrity between Item.ParentID and Item.ID but no cascade rules
                    // therefore, we need to delete the items in a folder in the proper order before deleting the folder itself
                    bool deleted = DeleteItems(requestedFolder);

                    // remove the current folder 
                    this.StorageContext.Folders.Remove(requestedFolder);
                    if (this.StorageContext.SaveChanges() < 1)
                    {
                        TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                        return ReturnResult<Folder>(req, operation, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        TraceLog.TraceInfo("Accepted");
                        return ReturnResult<Folder>(req, operation, requestedFolder, HttpStatusCode.Accepted);
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("Internal Server Error (database operation did not succeed)", ex);
                    return ReturnResult<Folder>(req, operation, HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex)
            {
                // Folder not found - it may have been deleted by someone else.  Return 200 OK.
                TraceLog.TraceException("Exception in database operation, return OK", ex);
                return ReturnResult<Folder>(req, operation, HttpStatusCode.OK);
            }
        }

        [WebGet(UriTemplate = "")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<Folder>> GetFolders(HttpRequestMessage req)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return ReturnResult<List<Folder>>(req, operation, code);

            try
            {
                var folders = this.StorageContext.Folders.Include("FolderUsers").
                    Include("Items.ItemTags").
                    Include("Items.FieldValues").
                    Where(f => f.UserID == CurrentUser.ID).ToList();

                var response = ReturnResult<List<Folder>>(req, operation, folders, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                return response;
            }
            catch (Exception ex)
            {
                // folders not found - return 404 Not Found
                TraceLog.TraceException("Resource not found", ex);
                return ReturnResult<List<Folder>>(req, operation, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Folder> GetFolder(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return ReturnResult<Folder>(req, operation, code);

            try
            {
                Folder requestedFolder = this.StorageContext.Folders.Include("FolderUsers").Include("Items.ItemTags").Include("Items.FieldValues").Single<Folder>(f => f.ID == id);

                // if the requested user is not the same as the authenticated user, return 403 Forbidden
                if (requestedFolder.UserID != CurrentUser.ID)
                {
                    TraceLog.TraceError("Entity does not belong to current user");
                    return ReturnResult<Folder>(req, operation, HttpStatusCode.Forbidden);
                }
                else
                {
                    var response = ReturnResult<Folder>(req, operation, requestedFolder, HttpStatusCode.OK);
                    response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                    return response;
                }
            }
            catch (Exception ex)
            {
                // folder not found - return 404 Not Found
                TraceLog.TraceException("Rsource not found", ex);
                return ReturnResult<Folder>(req, operation, HttpStatusCode.NotFound);
            }
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Folder> InsertFolder(HttpRequestMessage req)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Folder>(req, operation, code);
            }

            Folder clientFolder = null;
            code = ProcessRequestBody<Folder>(req, out clientFolder, out operation);
            if (code != HttpStatusCode.OK)  // error encountered processing body
                return ReturnResult<Folder>(req, operation, code);

            // default ItemTypeID to Task if not set
            if (clientFolder.ItemTypeID == null || clientFolder.ItemTypeID == Guid.Empty)
                clientFolder.ItemTypeID = SystemItemTypes.Task;

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
                if (folder == null || this.StorageContext.SaveChanges() < 1)
                {
                    TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                    return ReturnResult<Folder>(req, operation, HttpStatusCode.InternalServerError);
                }
                else
                {
                    TraceLog.TraceInfo("Created");
                    return ReturnResult<Folder>(req, operation, folder, HttpStatusCode.Created);
                }
            }
            catch (Exception ex)
            {
                // check for the condition where the folder is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbFolder = this.StorageContext.Folders.Single(g => g.ID == clientFolder.ID);
                    if (dbFolder.Name == clientFolder.Name &&
                        dbFolder.UserID == clientFolder.UserID)
                    {
                        TraceLog.TraceInfo("Accepted, entity exists : Exception[" + ex.Message + "]");
                        return ReturnResult<Folder>(req, operation, dbFolder, HttpStatusCode.Accepted);
                    }
                    else
                    {
                        TraceLog.TraceException("Conflict, entity in database did not match", ex);
                        return ReturnResult<Folder>(req, operation, HttpStatusCode.Conflict);
                    }
                }
                catch (Exception e)
                {
                    // folder not inserted - return 409 Conflict
                    TraceLog.TraceException(String.Format("Conflict, entity was not in database : Exception[{0}]", ex.Message), e);
                    return ReturnResult<Folder>(req, operation, HttpStatusCode.Conflict);
                }
            }
        }

        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Folder> UpdateFolder(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return ReturnResult<Folder>(req, operation, code);

            // the body will be two Folders - the original and the new values.  Verify this
            List<Folder> clientFolders = null;
            code = ProcessRequestBody<List<Folder>>(req, out clientFolders, out operation);
            if (code != HttpStatusCode.OK)  // error encountered processing body
                return ReturnResult<Folder>(req, operation, code);

            // get the original and new Folders out of the message body
            Folder originalFolder = clientFolders[0];
            Folder newFolder = clientFolders[1];

            // make sure the Folder ID's match
            if (originalFolder.ID != id || newFolder.ID != id)
            {
                TraceLog.TraceError("ID in URL does not match entity body)");
                return ReturnResult<Folder>(req, operation, HttpStatusCode.BadRequest);
            }

            try
            {
                Folder requestedFolder = this.StorageContext.Folders.Single<Folder>(t => t.ID == id);

                // if the Folder does not belong to the authenticated user, return 403 Forbidden
                if (requestedFolder.UserID != CurrentUser.ID)
                {
                    TraceLog.TraceError("Entity does not belong to current user)");
                    return ReturnResult<Folder>(req, operation, HttpStatusCode.Forbidden);
                }

                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalFolder.UserID = requestedFolder.UserID;
                newFolder.UserID = requestedFolder.UserID;

                bool changed = Update(requestedFolder, originalFolder, newFolder);
                if (changed == true)
                {
                    if (this.StorageContext.SaveChanges() < 1)
                    {
                        TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                        return ReturnResult<Folder>(req, operation, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        TraceLog.TraceInfo("Accepted");
                        return ReturnResult<Folder>(req, operation, requestedFolder, HttpStatusCode.Accepted);
                    }
                }
                else
                {
                    TraceLog.TraceInfo("Accepted (no changes)");
                    return ReturnResult<Folder>(req, operation, requestedFolder, HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {
                // Folder not found - return 404 Not Found
                TraceLog.TraceException("Resource not Found", ex);
                return ReturnResult<Folder>(req, operation, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Delete all items in a folder.  While there are any items, we use the ItemResource's recursive
        /// delete static methods to delete each item and any potential children.  This way we ensure that 
        /// the deletes are done in the proper order.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private bool DeleteItems(Folder folder)
        {
            bool multipleItemsDeleted = false;
            while (StorageContext.Items.Any(i => i.FolderID == folder.ID))
            {
                var item = StorageContext.Items.First(i => i.FolderID == folder.ID);
                // delete all the items with ParentID of this item.ID (recursively, from the bottom up)
                multipleItemsDeleted = ItemResource.DeleteItemChildrenRecursively(StorageContext, item);
                // delete all ItemRef FieldValues with Value of this item.ID
                multipleItemsDeleted |= ItemResource.DeleteItemReferences(CurrentUser, StorageContext, item);
                StorageContext.Items.Remove(item);
                StorageContext.SaveChanges();
            }
            return multipleItemsDeleted;
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
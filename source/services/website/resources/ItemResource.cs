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
    using BuiltSteady.Zaplify.Website.Helpers;

    [ServiceContract]
    [LogMessages]
    public class ItemResource : BaseResource
    {

        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> DeleteItem(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<Item>(req, code);
            }

            // get the item from the message body if one was passed
            Item clientItem;
            if (req.Content.Headers.ContentLength > 0)
            {
                clientItem = ProcessRequestBody(req, typeof(Item)) as Item;
                if (clientItem.ID != id)
                {   // IDs must match
                    LoggingHelper.TraceError("ItemResource.Delete: Bad Request (ID in URL does not match entity body)");
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                // otherwise get the client item from the database
                try
                {
                    clientItem = this.StorageContext.Items.Single<Item>(i => i.ID == id);
                }
                catch (Exception)
                {   // item not found - it may have been deleted by someone else.  Return 200 OK.
                    LoggingHelper.TraceInfo("ItemResource.Delete: entity not found; returned OK anyway");
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.OK);
                }
            }

            if (clientItem.UserID == null || clientItem.UserID == Guid.Empty)
            {   // changing a system Item to a user Item
                clientItem.UserID = CurrentUser.ID;
            }
            if (clientItem.UserID != CurrentUser.ID)
            {   // requested Item does not belong to authenticated user, return 403 Forbidden
                LoggingHelper.TraceError("ItemResource.Delete: Forbidden (entity does not belong to current user)");
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
            }

            try
            {
                Folder folder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == clientItem.FolderID);
                if (folder.UserID != CurrentUser.ID)
                {   // requested item does not belong to the authenticated user, return 403 Forbidden
                    LoggingHelper.TraceError("ItemResource.Delete: Forbidden (entity's folder does not belong to current user)");
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
                }

                try
                {
                    Item requestedItem = this.StorageContext.Items.Include("ItemTags").Include("FieldValues").Single<Item>(t => t.ID == id);

                    // delete all the itemtags associated with this item
                    if (requestedItem.ItemTags != null && requestedItem.ItemTags.Count > 0)
                    {
                        foreach (var tt in requestedItem.ItemTags.ToList())
                            this.StorageContext.ItemTags.Remove(tt);
                    }

                    // delete all the fieldvalues associated with this item
                    if (requestedItem.FieldValues != null && requestedItem.FieldValues.Count > 0)
                    {
                        foreach (var fv in requestedItem.FieldValues.ToList())
                            this.StorageContext.FieldValues.Remove(fv);
                    }

                    this.StorageContext.Items.Remove(requestedItem);
                    if (this.StorageContext.SaveChanges() < 1)
                    {
                        LoggingHelper.TraceError("ItemResource.Delete: Internal Server Error (database operation did not succeed)");
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        LoggingHelper.TraceInfo("ItemResource.Delete: Accepted");
                        return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.Accepted);
                    }
                }
                catch (Exception ex)
                {   // item not found - it may have been deleted by someone else.  Return 200 OK.
                    LoggingHelper.TraceInfo(String.Format("ItemResource.Delete: exception in database operation: {0}; returned OK anyway", ex.Message));
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                LoggingHelper.TraceError(String.Format("ItemResource.Delete: Not Found (folder not found); ex: " + ex.Message));
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> GetItem(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<Item>(req, code);
            }

            try
            {
                Item requestedItem = this.StorageContext.Items.Include("ItemTags").Include("FieldValues").Single<Item>(t => t.ID == id);

                // get the folder of the requested item
                try
                {
                    Folder folder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == requestedItem.FolderID);
                    if (folder.UserID != CurrentUser.ID || requestedItem.UserID != CurrentUser.ID)
                    {   // requested item does not belong to the authenticated user, return 403 Forbidden
                        LoggingHelper.TraceError("ItemResource.GetItem: Forbidden (entity does not belong to current user)");
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
                    }
                    else
                    {
                        var response = new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.OK);
                        response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                        return response;
                    }
                }
                catch (Exception ex)
                {   // folder not found - return 404 Not Found
                    LoggingHelper.TraceError("ItemResource.GetItem: Not Found (folder); ex: " + ex.Message);
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {   // item not found - return 404 Not Found
                LoggingHelper.TraceError("ItemResource.GetItem: Not Found (item); ex: " + ex.Message);
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
            }
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> InsertItem(HttpRequestMessage req)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<Item>(req, code);
            }

            // get the new item from the message body
            Item clientItem = ProcessRequestBody(req, typeof(Item)) as Item;

            if (clientItem.UserID == null || clientItem.UserID == Guid.Empty)
            {   // changing a system Item to a user Item
                clientItem.UserID = CurrentUser.ID;
            }
            if (clientItem.UserID != CurrentUser.ID)
            {   // requested Item does not belong to authenticated user, return 403 Forbidden
                LoggingHelper.TraceError("ItemResource.Insert: Forbidden (entity does not belong to current user)");
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
            }

            if (clientItem.ParentID == Guid.Empty)
            {   // parent ID is an empty guid, make it null instead so as to not violate ref integrity rules
                clientItem.ParentID = null;
            }

            try
            {
                Folder folder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == clientItem.FolderID);
                if (folder.UserID != CurrentUser.ID)
                {   // requested folder does not belong to the authenticated user, return 403 Forbidden
                    LoggingHelper.TraceError("ItemResource.Insert: Forbidden (entity's folder does not belong to current user)");
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
                }

                // fill out the ID if it's not set (e.g. from a javascript client)
                if (clientItem.ID == null || clientItem.ID == Guid.Empty)
                {
                    clientItem.ID = Guid.NewGuid();
                }

                // fill out the timestamps if they aren't set (null, or MinValue.Date, allowing for DST and timezone issues)
                DateTime now = DateTime.UtcNow;
                if (clientItem.Created == null || clientItem.Created.Date == DateTime.MinValue.Date)
                    clientItem.Created = now;
                if (clientItem.LastModified == null || clientItem.LastModified.Date == DateTime.MinValue.Date)
                    clientItem.LastModified = now;

                try
                {   // add the new item to the database
                    var item = this.StorageContext.Items.Add(clientItem);
                    if (this.StorageContext.SaveChanges() < 1 || item == null)
                    {
                        LoggingHelper.TraceError("ItemResource.Insert: Internal Server Error (database operation did not succeed)");
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.InternalServerError);  // return 500 Internal Server Error
                    }
                    else
                    {
                        // queue up the item for processing by the workflow worker
                        MessageQueue.EnqueueMessage(item.ID);
                        LoggingHelper.TraceInfo("ItemResource.Insert: Created");
                        return new HttpResponseMessageWrapper<Item>(req, item, HttpStatusCode.Created);     // return 201 Created
                    }
                }
                catch (Exception ex)
                {   // check for the condition where the item is already in the database
                    // in that case, return 202 Accepted; otherwise, return 409 Conflict
                    try
                    {
                        var dbItem = this.StorageContext.Items.Single(t => t.ID == clientItem.ID);
                        if (dbItem.Name == clientItem.Name)
                        {
                            LoggingHelper.TraceInfo("ItemResource.Insert: Accepted (entity already in database); ex: " + ex.Message);
                            return new HttpResponseMessageWrapper<Item>(req, dbItem, HttpStatusCode.Accepted);
                        }
                        else
                        {
                            LoggingHelper.TraceError("ItemResource.Insert: Conflict (entity in database did not match); ex: " + ex.Message);
                            return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Conflict);
                        }
                    }
                    catch (Exception e)
                    {   // item not inserted - return 409 Conflict
                        LoggingHelper.TraceError(String.Format("ItemResource.Insert: Conflict (entity was not in database); ex: {0}, ex {1}", ex.Message, e.Message));
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Conflict);
                    }
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                LoggingHelper.TraceError(String.Format("ItemResource.Delete: Not Found (folder); ex: " + ex.Message));
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
            }
        }
    
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> UpdateItem(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<Item>(req, code);
            }

            List<Item> clientItems = ProcessRequestBody(req, typeof(List<Item>)) as List<Item>;
            if (clientItems.Count != 2)
            {   // body should contain two items, the orginal and new values
                LoggingHelper.TraceError("ItemResource.Update: Bad Request (malformed body)");
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);
            }

            Item originalItem = clientItems[0];
            Item newItem = clientItems[1];

            // make sure the item ID's match
            if (originalItem.ID != newItem.ID)
            {
                LoggingHelper.TraceError("ItemResource.Update: Bad Request (original and new entity ID's do not match)");
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);
            }
            if (originalItem.ID != id)
            {
                LoggingHelper.TraceError("ItemResource.Update: Bad Request (ID in URL does not match entity body)");
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);
            }

            // if parent ID is an empty guid, make it null instead so as to not violate ref integrity rules
            if (originalItem.ParentID == Guid.Empty)
                originalItem.ParentID = null;
            if (newItem.ParentID == Guid.Empty)
                newItem.ParentID = null;

            if (newItem.LastModified.Year == 1970)
            {   // web client will set LastModified year to 1970 to get server timestamp
                newItem.LastModified = DateTime.Now;
            }

            // get the folder for the item
            try
            {
                Item requestedItem = this.StorageContext.Items.Include("ItemTags").Include("FieldValues").Single<Item>(t => t.ID == id);

                // if the Folder does not belong to the authenticated user, return 403 Forbidden
                if (requestedItem.UserID != CurrentUser.ID)
                {
                    LoggingHelper.TraceError("ItemResource.Update: Forbidden (entity does not belong to current user)");
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
                }
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalItem.UserID = requestedItem.UserID;
                newItem.UserID = requestedItem.UserID;

                Folder originalFolder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == originalItem.FolderID);
                Folder newFolder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == newItem.FolderID);

                if (originalFolder.UserID != CurrentUser.ID || newFolder.UserID != CurrentUser.ID ||
                    originalItem.UserID != CurrentUser.ID || newItem.UserID != CurrentUser.ID)
                {   // folder or item does not belong to the authenticated user, return 403 Forbidden
                    LoggingHelper.TraceError("ItemResource.Update: Forbidden (entity's folder does not belong to current user)");
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
                }

                try
                {
                    bool changed = false;
                    
                    if (requestedItem.ItemTags != null && requestedItem.ItemTags.Count > 0)
                    {   // delete all the itemtags associated with this item
                        foreach (var tt in requestedItem.ItemTags.ToList())
                            this.StorageContext.ItemTags.Remove(tt);
                        changed = true;
                    }

                    
                    if (requestedItem.FieldValues != null && requestedItem.FieldValues.Count > 0)
                    {   // delete all the fieldvalues associated with this item
                        foreach (var fv in requestedItem.FieldValues.ToList())
                            this.StorageContext.FieldValues.Remove(fv);
                        changed = true;
                    }

                    // call update and make sure the changed flag reflects the outcome correctly
                    changed = (Update(requestedItem, originalItem, newItem) == true ? true : changed);
                    if (changed == true)
                    {
                        if (this.StorageContext.SaveChanges() < 1)
                        {
                            LoggingHelper.TraceError("ItemResource.Update: Internal Server Error (database operation did not succeed)");
                            return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.InternalServerError);
                        }
                        else
                        {
                            LoggingHelper.TraceInfo("ItemResource.Update: Accepted");
                            return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.Accepted);
                        }
                    }
                    else
                    {
                        LoggingHelper.TraceInfo("ItemResource.Update: Accepted (no changes)");
                        return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.Accepted);
                    }
                }
                catch (Exception ex)
                {   // item not found - return 404 Not Found
                    LoggingHelper.TraceError("ItemResource.Update: Not Found (item); ex: " + ex.Message);
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                LoggingHelper.TraceError("ItemResource.Update: Not Found (folder); ex: " + ex.Message);
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
            }
        }

        private bool Update(Item requestedItem, Item originalItem, Item newItem)
        {
            bool updated = false;
            Type t = requestedItem.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedItem, null);
                object origValue = pi.GetValue(originalItem, null);
                object newValue = pi.GetValue(newItem, null);

                if (pi.Name == "ItemTags")
                {   // if this is the ItemTags field make it simple - if this update is the last one, it wins
                    if (newItem.LastModified > requestedItem.LastModified)
                    {
                        pi.SetValue(requestedItem, newValue, null);
                        updated = true;
                    }
                    continue;
                }

                // BUGBUG: this is too simplistic - should iterate thru fieldvalue collection and do finer-grained conflict management
                if (pi.Name == "FieldValues")
                {   // if this is the FieldValues field make it simple - if this update is the last one, it wins
                    if (newItem.LastModified > requestedItem.LastModified)
                    {
                        pi.SetValue(requestedItem, newValue, null);
                        updated = true;
                    }
                    continue;
                }

                if (!object.Equals(origValue, newValue))
                {   // value has changed, process further
                    if (object.Equals(serverValue, origValue) || newItem.LastModified > requestedItem.LastModified)
                    {   // server has the original value, or the new item has a later timestamp than the server, make the update
                        pi.SetValue(requestedItem, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}
namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.Website.Models;
    using BuiltSteady.Zaplify.ServerEntities;

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

            // get the new item from the message body
            Item clientItem = ProcessRequestBody(req, typeof(Item)) as Item;
            if (clientItem.ID != id)
            {   // IDs must match
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);
            }

            if (clientItem.UserID == null || clientItem.UserID == Guid.Empty)
            {   // changing a system Item to a user Item
                clientItem.UserID = CurrentUserID;
            }
            if (clientItem.UserID != CurrentUserID)
            {   // requested Item does not belong to authenticated user, return 403 Forbidden
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
            }

            try
            {
                Folder folder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == clientItem.FolderID);
                if (folder.UserID != CurrentUserID)
                {   // requested item does not belong to the authenticated user, return 403 Forbidden
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
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.InternalServerError);
                    }
                    return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.Accepted);
                }
                catch (Exception)
                {   // item not found - it may have been deleted by someone else.  Return 200 OK.
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.OK);
                }
            }
            catch (Exception)
            {   // folder not found - return 404 Not Found
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
                    if (folder.UserID != CurrentUserID || requestedItem.UserID != CurrentUserID)
                    {   // requested item does not belong to the authenticated user, return 403 Forbidden
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
                    }
                    return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.OK);
                }
                catch (Exception)
                {   // folder not found - return 404 Not Found
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
                }
            }
            catch (Exception)
            {   // item not found - return 404 Not Found
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
                clientItem.UserID = CurrentUserID;
            }
            if (clientItem.UserID != CurrentUserID)
            {   // requested Item does not belong to authenticated user, return 403 Forbidden
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
            }

            if (clientItem.ParentID == Guid.Empty)
            {   // parent ID is an empty guid, make it null instead so as to not violate ref integrity rules
                clientItem.ParentID = null;
            }

            try
            {
                Folder folder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == clientItem.FolderID);
                if (folder.UserID != CurrentUserID)
                {   // requested folder does not belong to the authenticated user, return 403 Forbidden
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
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Conflict);      // return 409 Conflict
                    }
                    return new HttpResponseMessageWrapper<Item>(req, item, HttpStatusCode.Created);     // return 201 Created
                }
                catch (Exception)
                {   // check for the condition where the folder is already in the database
                    // in that case, return 202 Accepted; otherwise, return 409 Conflict
                    try
                    {
                        var dbItem = this.StorageContext.Items.Single(t => t.ID == clientItem.ID);
                        if (dbItem.Name == clientItem.Name)
                        {
                            return new HttpResponseMessageWrapper<Item>(req, dbItem, HttpStatusCode.Accepted);
                        }
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Conflict);
                    }
                    catch (Exception)
                    {   // folder not inserted - return 409 Conflict
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Conflict);
                    }
                }
            }
            catch (Exception)
            {   // folder not found - return 404 Not Found
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
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);
            }

            Item originalItem = clientItems[0];
            Item newItem = clientItems[1];

            // make sure the item ID's match
            if (originalItem.ID != newItem.ID || originalItem.ID != id)
            {
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);
            }

            // if parent ID is an empty guid, make it null instead so as to not violate ref integrity rules
            if (originalItem.ParentID == Guid.Empty)
                originalItem.ParentID = null;
            if (newItem.ParentID == Guid.Empty)
                newItem.ParentID = null;

            // get the folder for the item
            try
            {
                Item requestedItem = this.StorageContext.Items.Include("ItemTags").Include("FieldValues").Single<Item>(t => t.ID == id);

                // if the Folder does not belong to the authenticated user, return 403 Forbidden
                if (requestedItem.UserID != CurrentUserID)
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalItem.UserID = requestedItem.UserID;
                newItem.UserID = requestedItem.UserID;

                Folder originalFolder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == originalItem.FolderID);
                Folder newFolder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == newItem.FolderID);

                if (originalFolder.UserID != CurrentUserID || newFolder.UserID != CurrentUserID ||
                    originalItem.UserID != CurrentUserID || newItem.UserID != CurrentUserID)
                {   // folder or item does not belong to the authenticated user, return 403 Forbidden
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
                            return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.InternalServerError);
                        }
                    }

                    return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.Accepted);
                }
                catch (Exception)
                {   // item not found - return 404 Not Found
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
                }
            }
            catch (Exception)
            {   // folder not found - return 404 Not Found
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
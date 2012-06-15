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
    using System.Web;

    [ServiceContract]
    [LogMessages]
    public class ItemResource : BaseResource
    {

        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> DeleteItem(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Item>(req, operation, code);
            }

            // get the item from the message body if one was passed
            Item clientItem;
            if (req.Content.Headers.ContentLength > 0)
            {
                clientItem = null;
                code = ProcessRequestBody(req, out clientItem, out operation);
                if (code != HttpStatusCode.OK)  // error encountered processing body
                    return ReturnResult<Item>(req, operation, code);

                if (clientItem.ID != id)
                {   // IDs must match
                    TraceLog.TraceError("ItemResource.Delete: Bad Request (ID in URL does not match entity body)");
                    return ReturnResult<Item>(req, operation, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                // otherwise get the client item from the database
                try
                {
                    clientItem = this.StorageContext.Items.Single<Item>(i => i.ID == id);
                    operation = this.StorageContext.CreateOperation(CurrentUser, req.Method.Method, null, clientItem, null);
                }
                catch (Exception)
                {   // item not found - it may have been deleted by someone else.  Return 200 OK along with a dummy item.
                    TraceLog.TraceInfo("ItemResource.Delete: entity not found; returned OK anyway");
                    return ReturnResult<Item>(req, operation, new Item() { Name = "Item Not Found" }, HttpStatusCode.OK);
                }
            }

            try
            {
                Folder folder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == clientItem.FolderID);
                if (folder.UserID != CurrentUser.ID)
                {   // requested item does not belong to the authenticated user, return 403 Forbidden
                    TraceLog.TraceError("ItemResource.Delete: Forbidden (entity's folder does not belong to current user)");
                    return ReturnResult<Item>(req, operation, HttpStatusCode.Forbidden);
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

                    bool multipleItemsDeleted = false;
                    // delete all the items with ParentID of this item.ID (recursively, from the bottom up)
                    multipleItemsDeleted = DeleteItemChildrenRecursively(StorageContext, requestedItem);
                    // delete all ItemRef FieldValues with Value of this item.ID
                    multipleItemsDeleted |= DeleteItemReferences(CurrentUser, StorageContext, requestedItem);

                    // process the delete
                    ItemProcessor ip = ItemProcessor.Create(StorageContext, CurrentUser, requestedItem.ItemTypeID);
                    if (ip != null)
                    {   // do itemtype-specific processing
                        ip.ProcessDelete(requestedItem);
                    }

                    // TODO: indicate using TimeStamp that multiple items were deleted

                    this.StorageContext.Items.Remove(requestedItem);
                    if (this.StorageContext.SaveChanges() < 1)
                    {
                        TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                        return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        if (folder.Name.StartsWith("$") == false)
                            WorkflowHost.WorkflowHost.InvokeWorkflowForOperation(this.StorageContext, null, operation);
                        TraceLog.TraceInfo("Accepted");
                        return ReturnResult<Item>(req, operation, requestedItem, HttpStatusCode.Accepted);
                    }
                }
                catch (Exception ex)
                {   // item not found - it may have been deleted by someone else.  Return 200 OK along with a dummy item.
                    TraceLog.TraceInfo(String.Format("Exception in database operation, return OK : Exception[{0}]", ex.Message));
                    return ReturnResult<Item>(req, operation, new Item() { Name = "Item Not Found" }, HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                TraceLog.TraceException(String.Format("Resource not found (Folder)"), ex);
                return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> GetItem(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Item>(req, operation, code);
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
                        TraceLog.TraceError("Entity does not belong to current user");
                        return ReturnResult<Item>(req, operation, HttpStatusCode.Forbidden);
                    }
                    else
                    {
                        var response = ReturnResult<Item>(req, operation, requestedItem, HttpStatusCode.OK);
                        response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                        return response;
                    }
                }
                catch (Exception ex)
                {   // folder not found - return 404 Not Found
                    TraceLog.TraceException("Resource not found (Folder)", ex);
                    return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {   // item not found - return 404 Not Found
                TraceLog.TraceException("Resource not found (Item)", ex);
                return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
            }
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> InsertItem(HttpRequestMessage req)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Item>(req, operation, code);
            }

            // get the new item from the message body
            Item clientItem = null;
            code = ProcessRequestBody<Item>(req, out clientItem, out operation);
            if (code != HttpStatusCode.OK)  // error encountered processing body
                return ReturnResult<Item>(req, operation, code);

            if (clientItem.ParentID == Guid.Empty)
            {   // parent ID is an empty guid, make it null instead so as to not violate ref integrity rules
                clientItem.ParentID = null;
            }

            try
            {
                Folder folder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == clientItem.FolderID);
                if (folder.UserID != CurrentUser.ID)
                {   // requested folder does not belong to the authenticated user, return 403 Forbidden
                    TraceLog.TraceError("Folder of Entity does not belong to current user)");
                    return ReturnResult<Item>(req, operation, HttpStatusCode.Forbidden);
                }

                // fill out the ID's for any FieldValues that travelled with the item
                if (clientItem.FieldValues != null)
                {
                    foreach (var fv in clientItem.FieldValues)
                        if (fv.ItemID == null || fv.ItemID == Guid.Empty)
                            fv.ItemID = clientItem.ID;
                }

                // fill out the timestamps if they aren't set (null, or MinValue.Date, allowing for DST and timezone issues)
                DateTime now = DateTime.UtcNow;
                if (clientItem.Created == null || clientItem.Created.Date == DateTime.MinValue.Date)
                    clientItem.Created = now;
                if (clientItem.LastModified == null || clientItem.LastModified.Date == DateTime.MinValue.Date)
                    clientItem.LastModified = now;

                ItemProcessor ip = ItemProcessor.Create(StorageContext, CurrentUser, clientItem.ItemTypeID);
                if (ip != null)
                {   // do itemtype-specific processing
                    ip.ProcessCreate(clientItem);
                }

                try
                {   // add the new item to the database
                    var item = this.StorageContext.Items.Add(clientItem);
                    int rows = this.StorageContext.SaveChanges();
                    if (rows < 1 || item == null)
                    {
                        TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                        return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);  // return 500 Internal Server Error
                    }
                    else
                    {
                        // invoke any workflows associated with this item
                        if (folder.Name.StartsWith("$") == false)
                            WorkflowHost.WorkflowHost.InvokeWorkflowForOperation(this.StorageContext, null, operation);
                        TraceLog.TraceInfo("Created");
                        return ReturnResult<Item>(req, operation, item, HttpStatusCode.Created);     // return 201 Created
                    }
                }
                catch (Exception ex)
                {   // check for the condition where the item is already in the database
                    // in that case, return 202 Accepted; otherwise, return 500 Internal Server Error
                    try
                    {
                        var dbItem = this.StorageContext.Items.Single(t => t.ID == clientItem.ID);
                        if (dbItem.Name == clientItem.Name)
                        {
                            TraceLog.TraceInfo("Accepted (entity already in database) : Exception[" + ex.Message + "]");
                            return ReturnResult<Item>(req, operation, dbItem, HttpStatusCode.Accepted);
                        }
                        else
                        {
                            TraceLog.TraceException("Error inserting entity", ex);
                            return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                        }
                    }
                    catch (Exception)
                    {   // item not inserted - return 500 Internal Server Error
                        TraceLog.TraceException("Error inserting entity", ex); 
                        return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                TraceLog.TraceException("Resource not found (Folder)", ex);
                return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
            }
        }
    
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> UpdateItem(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Item>(req, operation, code);
            }

            List<Item> clientItems = null;
            code = ProcessRequestBody<List<Item>>(req, out clientItems, out operation);
            if (code != HttpStatusCode.OK)  // error encountered processing body
                return ReturnResult<Item>(req, operation, code);

            Item originalItem = clientItems[0];
            Item newItem = clientItems[1];

            // make sure the item ID's match
            if (originalItem.ID != id || newItem.ID != id)
            {
                TraceLog.TraceError("ID in URL does not match entity body");
                return ReturnResult<Item>(req, operation, HttpStatusCode.BadRequest);
            }

            // if parent ID is an empty guid, make it null instead so as to not violate ref integrity rules
            if (originalItem.ParentID == Guid.Empty)
                originalItem.ParentID = null;
            if (newItem.ParentID == Guid.Empty)
                newItem.ParentID = null;

            if (newItem.LastModified.Year == 1970)
            {   // web client sets Date(0) to get server timestamp (ticks since 1970)
                newItem.LastModified = DateTime.UtcNow;
            }

            // get the folder for the item
            try
            {
                Item requestedItem = this.StorageContext.Items.Include("ItemTags").Include("FieldValues").Single<Item>(t => t.ID == id);

                // if the Folder does not belong to the authenticated user, return 403 Forbidden
                if (requestedItem.UserID != CurrentUser.ID)
                {
                    TraceLog.TraceError("Entity does not belong to current user)");
                    return ReturnResult<Item>(req, operation, HttpStatusCode.Forbidden);
                }
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalItem.UserID = requestedItem.UserID;
                newItem.UserID = requestedItem.UserID;

                Folder originalFolder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == originalItem.FolderID);
                Folder newFolder = this.StorageContext.Folders.Single<Folder>(tl => tl.ID == newItem.FolderID);

                if (originalFolder.UserID != CurrentUser.ID || newFolder.UserID != CurrentUser.ID ||
                    originalItem.UserID != CurrentUser.ID || newItem.UserID != CurrentUser.ID)
                {   // folder or item does not belong to the authenticated user, return 403 Forbidden
                    TraceLog.TraceError("Folder of Entity does not belong to current user)");
                    return ReturnResult<Item>(req, operation, HttpStatusCode.Forbidden);
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

                    ItemProcessor ip = ItemProcessor.Create(StorageContext, CurrentUser, newItem.ItemTypeID);
                    if (ip != null)
                    {   // do itemtype-specific processing
                        ip.ProcessUpdate(originalItem, newItem);
                    }

                    // call update and make sure the changed flag reflects the outcome correctly
                    changed = (Update(requestedItem, originalItem, newItem) == true ? true : changed);
                    if (changed == true)
                    {
                        int rows = this.StorageContext.SaveChanges();
                        if (rows < 0)
                        {
                            TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                            return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                        }
                        else
                        {
                            if (rows == 0)
                                TraceLog.TraceInfo("Inconsistency between the results of Update and zero rows affected");
                            if (newFolder.Name.StartsWith("$") == false)
                                WorkflowHost.WorkflowHost.InvokeWorkflowForOperation(this.StorageContext, null, operation);
                            TraceLog.TraceInfo("Accepted");
                            return ReturnResult<Item>(req, operation, requestedItem, HttpStatusCode.Accepted);
                        }
                    }
                    else
                    {
                        TraceLog.TraceInfo("Accepted (no changes)");
                        return ReturnResult<Item>(req, operation, requestedItem, HttpStatusCode.Accepted);
                    }
                }
                catch (Exception ex)
                {   // item not found - return 404 Not Found
                    TraceLog.TraceException("Resource not found (Item)", ex);
                    return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                TraceLog.TraceException("Resource not found (Folder)", ex);
                return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
            }
        }

        public static bool DeleteItemChildrenRecursively(UserStorageContext storageContext, Item item)
        {
            var children = storageContext.Items.Where(i => i.ParentID == item.ID).ToList();
            bool commit = false;
            foreach (var c in children)
            {
                DeleteItemChildrenRecursively(storageContext, c);
                storageContext.Items.Remove(c);
                commit = true;
            }

            // commit deletion of all children at the same layer together
            if (commit) { storageContext.SaveChanges(); }
            return commit;
        }

        public static bool DeleteItemReferences(User currentUser, UserStorageContext storageContext, Item item)
        {
            string itemID = item.ID.ToString();
            var itemRefs = storageContext.Items.Include("FieldValues").
                Where(i => i.UserID == currentUser.ID && i.ItemTypeID == SystemItemTypes.Reference &&
                      i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == itemID)).ToList();
            bool commit = false;
            foreach (var itemRef in itemRefs)
            {
                storageContext.Items.Remove(itemRef);
                commit = true;
            }

            // commit deletion of References
            if (commit) { storageContext.SaveChanges(); }
            return commit;
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

                // iterate thru fieldvalue collection and do finer-grained conflict management
                // the algorithm is to take a fieldvalue change only if the original value passed in is the same as 
                // the server's current value, OR the timestamp of the new item is more recent than the server's timestamp
                // the logic is complicated somewhat by taking into account NEW fieldvalues that don't exist on the original
                // item OR on the server's item.
                if (pi.Name == "FieldValues")
                {
                    var serverFVList = serverValue as List<FieldValue>;
                    var origFVList = origValue as List<FieldValue>;
                    var newFVList = newValue as List<FieldValue>;

                    // if there is no fieldvalue list on the new item, skip processing
                    // this logic assumes that fieldvalues are never removed from an item - only added
                    if (newFVList == null)
                        continue;

                    // iterate through the new item's fieldvalues
                    foreach (var newFV in newFVList)
                    {
                        FieldValue serverFV = null;
                        FieldValue origFV = null;
                        if (serverFVList != null && serverFVList.Any(fv => fv.FieldName == newFV.FieldName))
                            serverFV = serverFVList.Single(fv => fv.FieldName == newFV.FieldName);
                        if (origFVList != null && origFVList.Any(fv => fv.FieldName == newFV.FieldName))
                            origFV = origFVList.Single(fv => fv.FieldName == newFV.FieldName);

                        // if the value has changed, process further
                        if (origFV == null || origFV.Value != newFV.Value)
                        {
                            // process a new fieldvalue
                            if (origFV == null)
                            {
                                // if the server has no record, add this as a new fieldvalue
                                if (serverFV == null)
                                {
                                    serverFVList.Add(newFV);
                                    updated = true;
                                }
                                else
                                {
                                    // server also has this fieldvalue (so this is a conflict)
                                    // overwrite if this new item is newer than server's timestamp
                                    if (newItem.LastModified > requestedItem.LastModified)
                                    {
                                        serverFV.Value = newFV.Value;
                                        updated = true;
                                    }
                                }
                            }
                            else
                            {
                                // original and new values exist - process a fieldvalue update
                                if (serverFV == null)
                                {
                                    // this case shouldn't really happen - if the old and new items have this fieldvalue, the 
                                    // server should as well.  but we will tolerate this and add it anyway.
                                    serverFVList.Add(newFV);
                                    updated = true;
                                }
                                else
                                {
                                    // if server has the original value, or the new item has a later timestamp than the server, make the update
                                    if (origFV.Value == serverFV.Value || newItem.LastModified > requestedItem.LastModified)
                                    {
                                        serverFV.Value = newFV.Value;
                                        updated = true;
                                    }
                                }
                            }
                        }
                    }
                    continue;
                }

                // this logic applies for every field OTHER than Tags or FieldValues (e.g. Name, ItemTypeID, ParentID, FolderID, etc)
                if (!object.Equals(origValue, newValue))
                {   
                    // value has changed, process further
                    if (object.Equals(serverValue, origValue) || newItem.LastModified > requestedItem.LastModified)
                    {   
                        // server has the original value, or the new item has a later timestamp than the server, make the update
                        pi.SetValue(requestedItem, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}
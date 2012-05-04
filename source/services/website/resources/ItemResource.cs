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
    using BuiltSteady.Zaplify.ServiceUtilities.Supermarket;
    using BuiltSteady.Zaplify.Shared.Entities;

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

                    // remove all the items whose ParentID is this item (and do this recursively, from the bottom up)
                    DeleteItemChildrenRecursively(requestedItem);

                    this.StorageContext.Items.Remove(requestedItem);
                    if (this.StorageContext.SaveChanges() < 1)
                    {
                        TraceLog.TraceError("ItemResource.Delete: Internal Server Error (database operation did not succeed)");
                        return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        if (folder.Name.StartsWith("$") == false)
                            if (HostEnvironment.IsAzure)
                                MessageQueue.EnqueueMessage(operation.ID);
                        TraceLog.TraceInfo("ItemResource.Delete: Accepted");
                        return ReturnResult<Item>(req, operation, requestedItem, HttpStatusCode.Accepted);
                    }
                }
                catch (Exception ex)
                {   // item not found - it may have been deleted by someone else.  Return 200 OK along with a dummy item.
                    TraceLog.TraceInfo(String.Format("ItemResource.Delete: exception in database operation: {0}; returned OK anyway", ex.Message));
                    return ReturnResult<Item>(req, operation, new Item() { Name = "Item Not Found" }, HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                TraceLog.TraceException(String.Format("ItemResource.Delete: Not Found (folder not found)"), ex);
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
                        TraceLog.TraceError("ItemResource.GetItem: Forbidden (entity does not belong to current user)");
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
                    TraceLog.TraceException("ItemResource.GetItem: Not Found (folder)", ex);
                    return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {   // item not found - return 404 Not Found
                TraceLog.TraceException("ItemResource.GetItem: Not Found (item)", ex);
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
                    TraceLog.TraceError("ItemResource.Insert: Forbidden (entity's folder does not belong to current user)");
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

                // do itemtype-specific processing
                ProcessItem(clientItem);

                try
                {   // add the new item to the database
                    var item = this.StorageContext.Items.Add(clientItem);
                    int rows = this.StorageContext.SaveChanges();
                    if (rows < 1 || item == null)
                    {
                        TraceLog.TraceError("ItemResource.Insert: Internal Server Error (database operation did not succeed)");
                        return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);  // return 500 Internal Server Error
                    }
                    else
                    {
                        // queue up the item for processing by the workflow worker
                        if (folder.Name.StartsWith("$") == false)
                            if (HostEnvironment.IsAzure)
                                MessageQueue.EnqueueMessage(operation.ID);
                        TraceLog.TraceInfo("ItemResource.Insert: Created");
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
                            TraceLog.TraceInfo("ItemResource.Insert: Accepted (entity already in database); ex: " + ex.Message);
                            return ReturnResult<Item>(req, operation, dbItem, HttpStatusCode.Accepted);
                        }
                        else
                        {
                            TraceLog.TraceException("ItemResource.Insert: Error inserting entity", ex);
                            return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                        }
                    }
                    catch (Exception)
                    {   // item not inserted - return 500 Internal Server Error
                        TraceLog.TraceException("ItemResource.Insert: Error inserting entity", ex); 
                        return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                TraceLog.TraceException("ItemResource.Insert: Not Found (folder)", ex);
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
                TraceLog.TraceError("ItemResource.Update: Bad Request (ID in URL does not match entity body)");
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
                    TraceLog.TraceError("ItemResource.Update: Forbidden (entity does not belong to current user)");
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
                    TraceLog.TraceError("ItemResource.Update: Forbidden (entity's folder does not belong to current user)");
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
                            TraceLog.TraceError("ItemResource.Update: Internal Server Error (database operation did not succeed)");
                            return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                        }
                        else
                        {
                            if (newFolder.Name.StartsWith("$") == false)
                                if (HostEnvironment.IsAzure)
                                    MessageQueue.EnqueueMessage(operation.ID);
                            TraceLog.TraceInfo("ItemResource.Update: Accepted");
                            return ReturnResult<Item>(req, operation, requestedItem, HttpStatusCode.Accepted);
                        }
                    }
                    else
                    {
                        TraceLog.TraceInfo("ItemResource.Update: Accepted (no changes)");
                        return ReturnResult<Item>(req, operation, requestedItem, HttpStatusCode.Accepted);
                    }
                }
                catch (Exception ex)
                {   // item not found - return 404 Not Found
                    TraceLog.TraceException("ItemResource.Update: Not Found (item)", ex);
                    return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {   // folder not found - return 404 Not Found
                TraceLog.TraceException("ItemResource.Update: Not Found (folder)", ex);
                return ReturnResult<Item>(req, operation, HttpStatusCode.NotFound);
            }
        }

        void DeleteItemChildrenRecursively(Item item)
        {
            var children = this.StorageContext.Items.Where(i => i.ParentID == item.ID).ToList();
            bool removed = false;
            foreach (var c in children)
            {
                DeleteItemChildrenRecursively(c);
                this.StorageContext.Items.Remove(c);
                removed = true;
            }

            // remove all of the children at the same layer together
            if (removed)
                this.StorageContext.SaveChanges();
        }

        private void ProcessItem(Item item)
        {
            // do itemtype-specific processing on the item
            if (item.ItemTypeID == SystemItemTypes.ShoppingItem)
                ProcessShoppingItem(item);
        }

        private void ProcessShoppingItem(Item item)
        {
            // use the Supermarket API to get grocery category
            SupermarketAPI smApi = new SupermarketAPI();

#if FALSE   // use the synchronous codepath for now
            // execute the call asynchronously so as to not block the response
            smApi.BeginQuery(SupermarketQueries.SearchByProductName, item.Name, new AsyncCallback((iar) =>
            {
                try
                {
                    var results = smApi.EndQuery(iar);

                    // find the item using a new context 
                    var context = Storage.NewUserContext;
                    var shoppingItem = context.Items.Single(i => i.ID == item.ID);
                    FieldValue categoryFV = shoppingItem.GetFieldValue(FieldNames.Category, true);

                    // get the category
                    foreach (var entry in results)
                    {
                        categoryFV.Value = entry[SupermarketQueryResult.Category];
                        // only grab the first category
                        break;
                    }
                    context.SaveChanges();
                    TraceLog.TraceInfo(String.Format("ProcessShoppingItem: assigned {0} category to item {1}", categoryFV.Value, item.Name));
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("ProcessShoppingItem: Supermarket API or database commit failed", ex);
                }
            }), null);
#else
            try
            {
                var results = smApi.Query(SupermarketQueries.SearchByProductName, item.Name);
                FieldValue categoryFV = item.GetFieldValue(FieldNames.Category, true);

                // get the category
                foreach (var entry in results)
                {
                    categoryFV.Value = entry[SupermarketQueryResult.Category];
                    // only grab the first category
                    break;
                }
                this.StorageContext.SaveChanges();
                TraceLog.TraceInfo(String.Format("ProcessShoppingItem: assigned {0} category to item {1}", categoryFV.Value, item.Name));
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessShoppingItem: Supermarket API or database commit failed", ex);
            }
#endif
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
                    if (newItem.LastModified >= requestedItem.LastModified)
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
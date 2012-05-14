﻿namespace BuiltSteady.Zaplify.Website.Resources
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
    using BuiltSteady.Zaplify.ServiceUtilities.Supermarket;
    using BuiltSteady.Zaplify.Shared.Entities;
    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.ServiceUtilities.Grocery;

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

                    bool multipleItemsDeleted = false;
                    // delete all the items with ParentID of this item.ID (recursively, from the bottom up)
                    multipleItemsDeleted = DeleteItemChildrenRecursively(requestedItem);
                    // delete all ItemRef FieldValues with Value of this item.ID
                    multipleItemsDeleted |= DeleteItemReferences(requestedItem);

                    // TODO: indicate using TimeStamp that multiple items were deleted

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
                ProcessItemInsert(req, clientItem);

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

                    // do itemtype-specific processing
                    ProcessItemUpdate(req, originalItem, newItem);

                    // call update and make sure the changed flag reflects the outcome correctly
                    changed = (Update(requestedItem, originalItem, newItem) == true ? true : changed);
                    if (changed == true)
                    {
                        int rows = this.StorageContext.SaveChanges();
                        if (rows < 0)
                        {
                            TraceLog.TraceError("ItemResource.Update: Internal Server Error (database operation did not succeed)");
                            return ReturnResult<Item>(req, operation, HttpStatusCode.InternalServerError);
                        }
                        else
                        {
                            if (rows == 0)
                                TraceLog.TraceInfo("ItemResource.Update: inconsistency between the results of Update and zero rows affected");
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

        bool DeleteItemChildrenRecursively(Item item)
        {
            var children = this.StorageContext.Items.Where(i => i.ParentID == item.ID).ToList();
            bool commit = false;
            foreach (var c in children)
            {
                DeleteItemChildrenRecursively(c);
                this.StorageContext.Items.Remove(c);
                commit = true;
            }

            // commit deletion of all children at the same layer together
            if (commit) { this.StorageContext.SaveChanges(); }
            return commit;
        }

        bool DeleteItemReferences(Item item)
        {
            string itemID = item.ID.ToString();
            var itemRefs = this.StorageContext.Items.Include("FieldValues").
                Where(i => i.UserID == CurrentUser.ID && i.ItemTypeID == SystemItemTypes.Reference &&
                      i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == itemID)).ToList();
            bool commit = false;
            foreach (var itemRef in itemRefs)
            {
                this.StorageContext.Items.Remove(itemRef);
                commit = true;
            }

            // commit deletion of References
            if (commit) { this.StorageContext.SaveChanges(); }
            return commit;
        }

        private void ProcessItemInsert(HttpRequestMessage req, Item item)
        {
            // do itemtype-specific processing on the item
            if (item.ItemTypeID == SystemItemTypes.ShoppingItem)
                ProcessShoppingItemInsert(req, item);
        }

        private void ProcessShoppingItemInsert(HttpRequestMessage req, Item item)
        {
            // check if the user already set a category in the incoming item
            // in this case, simply overwrite the current category that's saved in $User/GroceryCategories
            FieldValue groceryCategoryFV = null;
            var categoryFV = item.GetFieldValue(FieldNames.Category);
            if (categoryFV != null)
            {
                groceryCategoryFV = GetGroceryCategoryItemFieldValue(item, true);
                if (groceryCategoryFV == null)
                    return;

                groceryCategoryFV.Value = categoryFV.Value;
                StorageContext.SaveChanges();
                return;
            }

            // create a new category fieldvalue in the item to process
            categoryFV = item.GetFieldValue(FieldNames.Category, true);
            
            // check if the grocery category has already been saved in $User/GroceryCategories
            // in this case, store the category in the incoming item
            groceryCategoryFV = GetGroceryCategoryItemFieldValue(item);
            if (groceryCategoryFV != null && groceryCategoryFV.Value != null)
            {
                categoryFV.Value = groceryCategoryFV.Value;
                StorageContext.SaveChanges();
                return;
            }

            // set up the grocery API endpoint
            GroceryAPI gApi = new GroceryAPI();
            if (HostEnvironment.IsAzure)
            {
                // set the proper endpoint URI based on the account name 
                var connStr = ConfigurationSettings.Get("DataConnectionString");
                var acctname = @"AccountName=";
                var start = connStr.IndexOf(acctname);
                if (start >= 0)
                {
                    var end = connStr.IndexOf(';', start);
                    var accountName = connStr.Substring(start + acctname.Length, end - start - acctname.Length);
                    var endpointBaseUri = String.Format("{0}://{1}.cloudapp.net/Grocery/", req.RequestUri.Scheme, accountName);
                    gApi.EndpointBaseUri = endpointBaseUri;
                    TraceLog.TraceDetail("ProcessShoppingItemInsert: endpointURI: " + endpointBaseUri);
                }
            }
            else
                if (req.RequestUri.Authority.StartsWith("localhost") || req.RequestUri.Authority.StartsWith("127.0.0.1"))
                {
                    // if debugging, set the proper endpoint URI
                    var endpointBaseUri = String.Format("{0}://{1}/Grocery/", req.RequestUri.Scheme, req.RequestUri.Authority);
                    gApi.EndpointBaseUri = endpointBaseUri;
                    TraceLog.TraceDetail("ProcessShoppingItemInsert: endpointURI: " + endpointBaseUri);
                }

            // try to find the category from the local Grocery Controller
            try
            {
                var results = gApi.Query(GroceryQueries.GroceryCategory, item.Name).ToList();

                // this should only return one result
                if (results.Count > 0)
                {
                    // get the category
                    foreach (var entry in results)
                    {
                        categoryFV.Value = entry[GroceryQueryResult.Category];
                        // only grab the first category
                        break;
                    }
                    this.StorageContext.SaveChanges();
                    TraceLog.TraceInfo(String.Format("ProcessShoppingItemInsert: Grocery API assigned {0} category to item {1}", categoryFV.Value, item.Name));
                    return;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessShoppingItemInsert: Grocery API or database commit failed", ex);
            }

            // last resort...
            // use the Supermarket API to get the grocery category
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

                // get the category
                foreach (var entry in results)
                {
                    categoryFV.Value = entry[SupermarketQueryResult.Category];

                    this.StorageContext.SaveChanges();
                    TraceLog.TraceInfo(String.Format("ProcessShoppingItemInsert: Supermarket API assigned {0} category to item {1}", categoryFV.Value, item.Name));
                    
                    // only grab the first category
                    break;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessShoppingItem: Supermarket API or database commit failed", ex);
            }
#endif
        }

        private void ProcessItemUpdate(HttpRequestMessage req, Item oldItem, Item newItem)
        {
            // do itemtype-specific processing on the item
            if (newItem.ItemTypeID == SystemItemTypes.ShoppingItem)
                ProcessShoppingItemUpdate(req, oldItem, newItem);
        }

        private void ProcessShoppingItemUpdate(HttpRequestMessage req, Item oldItem, Item newItem)
        {
            // if the user stored a grocery category, overwrite the current category that's saved in $User/GroceryCategories
            var categoryFV = newItem.GetFieldValue(FieldNames.Category);
            if (categoryFV != null && categoryFV.Value != null)
            {
                // the old category must not exist or the value must have changed
                var oldCategoryFV = oldItem.GetFieldValue(FieldNames.Category);
                if (oldCategoryFV == null || oldCategoryFV.Value != categoryFV.Value)
                {
                    // get the grocery category fieldvalue for the corresponding Item in $User/GroceryCategories
                    var groceryCategoryFV = GetGroceryCategoryItemFieldValue(newItem, true);
                    if (groceryCategoryFV == null)
                        return;

                    groceryCategoryFV.Value = categoryFV.Value;
                    StorageContext.SaveChanges();
                }
            }
        }

        private FieldValue GetGroceryCategoryItemFieldValue(Item item, bool create = false)
        {
            // get the grocery categories list under the $User folder
            var groceryCategories = StorageContext.GetOrCreateGroceryCategoriesList(CurrentUser);
            if (groceryCategories == null)
                return null;

            Item groceryCategory = null;
            FieldValue groceryCategoryFV = null;
            var itemName = item.Name.ToLower();
            if (StorageContext.Items.Any(i => i.Name == itemName && i.ParentID == groceryCategories.ID))
            {
                groceryCategory = StorageContext.Items.Include("FieldValues").Single(i => i.Name == item.Name && i.ParentID == groceryCategories.ID);
                groceryCategoryFV = groceryCategory.GetFieldValue(FieldNames.Value, true);
            }
            else if (create)
            {
                // create grocery category item 
                DateTime now = DateTime.UtcNow;
                var groceryCategoryItemID = Guid.NewGuid();
                groceryCategoryFV = new FieldValue()
                {
                    ItemID = groceryCategoryItemID,
                    FieldName = FieldNames.Value,
                    Value = null,
                };
                groceryCategory = new Item()
                {
                    ID = groceryCategoryItemID,
                    Name = itemName,
                    FolderID = groceryCategories.FolderID,
                    UserID = CurrentUser.ID,
                    ItemTypeID = SystemItemTypes.NameValue,
                    ParentID = groceryCategories.ID,
                    Created = now,
                    LastModified = now,
                    FieldValues = new List<FieldValue>() { groceryCategoryFV }
                };
                StorageContext.Items.Add(groceryCategory);
            }

            return groceryCategoryFV;
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